using System.Numerics;
using ImGuiNET;

namespace BurntToast;

internal static class ImGuiExtensions {
    internal static void TextColored(string text, Vector4 color) {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
        ImGui.PopStyleColor();
    }
}