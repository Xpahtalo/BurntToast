using System;
using System.Linq;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using XivCommon.Functions;

namespace BurntToast {
    public class Filter : IDisposable {
        private BurntToast Plugin { get; }

        internal Filter(BurntToast plugin) {
            this.Plugin = plugin;

            this.Plugin.ToastGui.Toast += this.OnToast;
            this.Plugin.ToastGui.QuestToast += this.OnQuestToast;
            this.Plugin.ToastGui.ErrorToast += this.OnErrorToast;
            this.Plugin.Common.Functions.BattleTalk.OnBattleTalk += this.OnBattleTalk;
        }

        public void Dispose() {
            this.Plugin.Common.Functions.BattleTalk.OnBattleTalk -= this.OnBattleTalk;
            this.Plugin.ToastGui.ErrorToast -= this.OnErrorToast;
            this.Plugin.ToastGui.QuestToast -= this.OnQuestToast;
            this.Plugin.ToastGui.Toast -= this.OnToast;
        }

        private bool AnyMatches(string text) {
            return this.Plugin.Config.Patterns.Any(regex => regex.IsMatch(text));
        }

        private void OnToast(ref SeString message, ref ToastOptions options, ref bool isHandled) {
            this.DoFilter(message, ref isHandled);
        }

        private void OnErrorToast(ref SeString message, ref bool isHandled) {
            this.DoFilter(message, ref isHandled);
        }

        private void OnQuestToast(ref SeString message, ref QuestToastOptions options, ref bool isHandled) {
            this.DoFilter(message, ref isHandled);
        }

        private void DoFilter(SeString message, ref bool isHandled) {
            if (isHandled) {
                return;
            }

            if (this.AnyMatches(message.TextValue)) {
                isHandled = true;
            }
        }

        private void OnBattleTalk(ref SeString sender, ref SeString message, ref BattleTalkOptions options, ref bool isHandled) {
            if (isHandled) {
                return;
            }

            var text = message.TextValue;
            var pattern = this.Plugin.Config.BattleTalkPatterns.Find(pattern => pattern.Pattern.IsMatch(text));
            if (pattern == null) {
                return;
            }

            isHandled = true;

            if (pattern.ShowMessage) {
                this.Plugin.ChatGui.Print(new XivChatEntry {
                    Type = (XivChatType) 68,
                    Name = sender.TextValue,
                    Message = message,
                });
            }
        }
    }
}
