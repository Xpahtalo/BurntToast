using System;
using System.Linq;
using Dalamud.Game.Internal.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;

namespace BurntToast {
    public class Filter : IDisposable {
        private BurntToast Plugin { get; }

        internal Filter(BurntToast plugin) {
            this.Plugin = plugin;

            this.Plugin.Interface.Framework.Gui.Toast.OnToast += this.OnToast;
            this.Plugin.Interface.Framework.Gui.Toast.OnQuestToast += this.OnQuestToast;
            this.Plugin.Interface.Framework.Gui.Toast.OnErrorToast += this.OnErrorToast;
        }

        public void Dispose() {
            this.Plugin.Interface.Framework.Gui.Toast.OnErrorToast -= this.OnErrorToast;
            this.Plugin.Interface.Framework.Gui.Toast.OnQuestToast -= this.OnQuestToast;
            this.Plugin.Interface.Framework.Gui.Toast.OnToast -= this.OnToast;
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

            var text = message.TextValue;
            if (this.Plugin.Config.Patterns.Any(regex => regex.IsMatch(text))) {
                isHandled = true;
            }
        }
    }
}
