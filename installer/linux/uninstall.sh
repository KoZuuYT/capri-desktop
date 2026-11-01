#!/bin/bash
# removes Capri (mod) for Linux; asks whether to keep her memories, settings and downloaded models
APP="$HOME/.local/share/capri-mod"
read -r -p "Remover a Capri (mod) deste computador? [s/N] " answer
case "$answer" in [sS]*) ;; *) exit 0 ;; esac
read -r -p "Apagar também as memórias, configurações e modelos baixados dela? [s/N] " everything
WINEPREFIX="$APP/wine" wineserver -k >/dev/null 2>&1 || true
rm -f "$HOME/.local/share/applications/capri-mod.desktop" "$HOME/.config/autostart/capri-mod.desktop" "$HOME/.local/bin/capri-mod" "$HOME/.local/bin/capri-mod-remover"
case "$everything" in
	[sS]*) rm -rf "$APP" ;;
	*) rm -f "$APP/wine/drive_c/users/$USER/AppData/Local/Capri/Capri.exe" "$APP/capri" "$APP/capri.png" "$APP/uninstall.sh" ;;
esac
echo "A Capri foi removida."
