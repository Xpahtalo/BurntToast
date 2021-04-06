using System;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling;

namespace Burnt_Toast {
    public class Filter : IDisposable {
        private BurntToast Plugin { get; }

        internal Filter(BurntToast plugin) {
            this.Plugin = plugin;

            this.Plugin.Interface.Framework.Gui.Toast.OnToast += this.OnToast;
        }

        public void Dispose() {
            this.Plugin.Interface.Framework.Gui.Toast.OnToast -= this.OnToast;
        }

        private void OnToast(ref SeString message, ref bool isHandled) {
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
