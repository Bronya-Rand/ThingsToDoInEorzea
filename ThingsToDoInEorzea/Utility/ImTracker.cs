using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ThingsToDoInEorzea.Utility
{
    public enum ObjectiveWindowTextAlign
    {
        Left,
        Center,
        Right,
    }

    public static class ObjectiveWindowTextAlignExtensions
    {
        public static string GetName(this ObjectiveWindowTextAlign align) => align switch
        {
            ObjectiveWindowTextAlign.Left => "Left",
            ObjectiveWindowTextAlign.Center => "Center",
            ObjectiveWindowTextAlign.Right => "Right",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Provides utility methods for rendering text and icons in ImGui
    /// </summary>
    public static class ImTracker
    {
        /// <summary>
        /// Renders outlined text with optional wrapping and alignment.
        /// </summary>
        /// <param name="text">The text to be rendered.</param>
        /// <param name="textColor">The color of the text.</param>
        /// <param name="outlineColor">The color of the text outline.</param>
        /// <param name="fontSize">The size of the font.</param>
        /// <param name="outlineThickness">The thickness of the text outline.</param>
        /// <param name="wrapWidth">The width at which to wrap the text.</param>
        /// <param name="align">The alignment of the text.</param>
        public static void OutlinedText(
            string text,
            Vector4 textColor,
            Vector4 outlineColor,
            float fontSize = 0.0f, // Use 0.0f for default font size
            float outlineThickness = 1.0f,
            float wrapWidth = 0.0f, // Use 0.0f for no wrapping
            ObjectiveWindowTextAlign align = ObjectiveWindowTextAlign.Left
        )
        {
            var drawList = ImGui.GetWindowDrawList();
            var font = ImGui.GetFont();
            var startPos = ImGui.GetCursorScreenPos();
            var startCursor = ImGui.GetCursorPos();

            var scale = ImGuiHelpers.GlobalScale;
            var effectiveFontSize =
                (fontSize > 0.0f ? fontSize : font.FontSize) * scale * AtkUnitBase.GetGlobalUIScale();
            var t = outlineThickness * scale;
            var textColorU32 = ImGui.GetColorU32(textColor);
            var outlineColorU32 = ImGui.GetColorU32(outlineColor);

            var lines = wrapWidth > 0.0f
                            ? WrapText(text, font, effectiveFontSize, wrapWidth)
                            : [text];

            var cursorY = startPos.Y;
            var totalHeight = 0f;
            var maxLineWidth = 0f;

            foreach (var line in lines)
            {
                var lineSize = ImGui.CalcTextSize(line);
                var lineWidth = lineSize.X;
                if (effectiveFontSize != font.FontSize && font.FontSize > 0.0f)
                    lineWidth *= effectiveFontSize / font.FontSize;

                if (lineWidth > maxLineWidth)
                    maxLineWidth = lineWidth;

                // For right alignment, derive X offset from available width
                var availableWidth = wrapWidth > 0.0f ? wrapWidth : lineWidth;
                var lineX = align switch
                {
                    ObjectiveWindowTextAlign.Center => startPos.X + ((availableWidth - lineWidth) / 2),
                    ObjectiveWindowTextAlign.Right => startPos.X + (availableWidth - lineWidth),
                    _ => startPos.X
                };
                var linePos = new Vector2(lineX, cursorY);

                // Draw outline in 8 directions
                drawList.AddText(font, effectiveFontSize, linePos + new Vector2(-t, -t), outlineColorU32, line);
                drawList.AddText(font, effectiveFontSize, linePos + new Vector2(0, -t), outlineColorU32, line);
                drawList.AddText(font, effectiveFontSize, linePos + new Vector2(t, -t), outlineColorU32, line);
                drawList.AddText(font, effectiveFontSize, linePos + new Vector2(-t, 0), outlineColorU32, line);
                drawList.AddText(font, effectiveFontSize, linePos + new Vector2(t, 0), outlineColorU32, line);
                drawList.AddText(font, effectiveFontSize, linePos + new Vector2(-t, t), outlineColorU32, line);
                drawList.AddText(font, effectiveFontSize, linePos + new Vector2(0, t), outlineColorU32, line);
                drawList.AddText(font, effectiveFontSize, linePos + new Vector2(t, t), outlineColorU32, line);

                // Draw the main text
                drawList.AddText(font, effectiveFontSize, linePos, textColorU32, line);

                var lineHeight = lineSize.Y;
                if (effectiveFontSize != font.FontSize && font.FontSize > 0.0)
                    lineHeight *= effectiveFontSize / font.FontSize;

                cursorY += lineHeight;
                totalHeight += lineHeight;
            }

            // Advance the cursor position for layout
            ImGui.SetCursorPos(startCursor);
            ImGui.Dummy(new Vector2(wrapWidth > 0.0f ? wrapWidth : maxLineWidth, totalHeight));
        }

        /// <summary>
        /// Adds spacing using ImGui.Dummy to create spacing of the specified size.
        /// </summary>
        /// <param name="spacing">The size of the spacing to add.</param>
        public static void Spacing(Vector2 spacing)
        {
            ImGui.Dummy(spacing);
        }

        /// <summary>
        /// Adds spacing around a separator for better visual separation.
        /// </summary>
        public static void SpacedSeparator()
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
        }

        public static List<string> WrapText(string text, ImFontPtr font, float fontSize, float wrapWidth)
        {
            var result = new List<string>();
            var words = text.Split(' ');
            var currentLine = string.Empty;

            foreach (var word in words)
            {
                var candidateLine = currentLine.IsNullOrEmpty() ? word : $"{currentLine} {word}";
                var candidateWidth = MeasureWidth(candidateLine, font, fontSize);

                if (candidateWidth <= wrapWidth || currentLine.IsNullOrEmpty())
                    currentLine = candidateLine;
                else
                {
                    result.Add(currentLine);
                    currentLine = word;
                }
            }

            if (!currentLine.IsNullOrEmpty())
                result.Add(currentLine);

            return result.Count == 0 ? [string.Empty] : result;
        }

        public static float MeasureWidth(string text, ImFontPtr font, float fontSize)
        {
            var raw = ImGui.CalcTextSize(text);
            if (fontSize != font.FontSize && font.FontSize > 0.0f)
                raw.X *= fontSize / font.FontSize;
            return raw.X;
        }

        /// <summary>
        /// Renders a clickable FontAwesome icon with hover color change and tooltip.
        /// Returns true if the icon was clicked.
        /// </summary>
        /// <param name="id">ImGui ID for the icon (must be unique, use ### prefix)</param>
        /// <param name="icon">FontAwesome icon to display</param>
        /// <param name="tooltip">Tooltip text to show on hover</param>
        /// <param name="activeColor">Color when not hovered (default: white)</param>
        /// <param name="hoverColor">Color when hovered (default: Dalamud yellow)</param>
        /// <returns>True if the icon was clicked</returns>
        public static bool DrawClickableIcon(
            string id,
            FontAwesomeIcon icon,
            string tooltip,
            Vector4? activeColor = null,
            Vector4? hoverColor = null)
        {
            var defaultColor = activeColor ?? new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            var highlightColor = hoverColor ?? ImGuiColors.DalamudYellow;

            var isClicked = false;

            using (ImRaii.PushId(id))
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                // Check hover state before drawing
                var cursorPos = ImGui.GetCursorScreenPos();
                var iconText = icon.ToIconString();
                var textSize = ImGui.CalcTextSize(iconText);

                // Use the hover color if mouse is over the icon area
                var mousePos = ImGui.GetMousePos();
                var isHovered = mousePos.X >= cursorPos.X && mousePos.X <= cursorPos.X + textSize.X &&
                                mousePos.Y >= cursorPos.Y && mousePos.Y <= cursorPos.Y + textSize.Y;

                var color = isHovered ? highlightColor : defaultColor;

                using (ImRaii.PushColor(ImGuiCol.Text, color))
                {
                    ImGui.Text(iconText);
                }

                // Check for click
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    isClicked = true;
                }

                // Show tooltip
                if (ImGui.IsItemHovered())
                {
                    using (ImRaii.PushFont(UiBuilder.DefaultFont))
                        ImGui.SetTooltip(tooltip);
                }
            }

            return isClicked;
        }

        /// <summary>
        /// Renders text centered within the available content region.
        /// </summary>
        /// <param name="text">The text to be rendered.</param>
        private static void CenterText(string text)
        {
            var textWidth = ImGui.CalcTextSize(text).X;
            var avail = ImGui.GetContentRegionAvail().X;
            var x = (avail - textWidth) / 2;
            if (x > 0f)
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + x);
        }

        public static void CenteredText(string text)
        {
            CenterText(text);
            ImGui.Text(text);
        }

        public static void CenteredTextDisabled(string text)
        {
            CenterText(text);
            ImGui.TextDisabled(text);
        }

        /// <summary>
        /// Returns a <see cref="ImGui.IsItemHovered(ImGuiHoveredFlags)"/> with the <see cref="ImGuiHoveredFlags.AllowWhenDisabled"/> flag set.
        /// </summary>
        /// <returns></returns>
        public static bool IsItemHoveredWhenDisabled()
        {
            return ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);
        }
    }
}
