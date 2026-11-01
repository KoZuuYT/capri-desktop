#!/bin/bash
# Capri (mod) for Linux: the Windows build running under Wine, installed for this user only.
# Run it with: bash Capri-Mod-Linux-<version>.sh
set -e
VERSION="@VERSION@"
APP="$HOME/.local/share/capri-mod"
PREFIX="$APP/wine"
LOCAL="$PREFIX/drive_c/users/$USER/AppData/Local/Capri"
MENU="$HOME/.local/share/applications/capri-mod.desktop"
AUTOSTART="$HOME/.config/autostart/capri-mod.desktop"

say() { printf '\n\033[1;35m%s\033[0m\n' "$*"; }

if [ "$(id -u)" = 0 ]; then
	echo "Rode sem sudo: bash $0 (ele pede a senha só quando precisar)."
	exit 1
fi
say "Capri (versão modificada $VERSION) para Linux"

# what she needs from the system: Wine runs her, parec lets her hear the call, Vulkan runs her brain on the GPU
need=()
command -v wine >/dev/null 2>&1 || need+=(wine)
command -v parec >/dev/null 2>&1 || need+=(pulseaudio-utils)
if command -v rpm >/dev/null 2>&1; then
	rpm -q vulkan-loader >/dev/null 2>&1 || need+=(vulkan-loader)
	rpm -q mesa-vulkan-drivers >/dev/null 2>&1 || need+=(mesa-vulkan-drivers)
fi
if [ ${#need[@]} -gt 0 ]; then
	if command -v dnf >/dev/null 2>&1; then
		say "Instalando o que falta: ${need[*]} (vai pedir sua senha)"
		sudo dnf install -y "${need[@]}"
	else
		echo "Instale estes pacotes e rode de novo: ${need[*]}"
		exit 1
	fi
fi

TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT
say "Abrindo os arquivos..."
start=$(awk '/^__PAYLOAD__$/ { print NR + 1; exit }' "$0")
tail -n +"$start" "$0" | tar -xz -C "$TMP"

export WINEPREFIX="$PREFIX" WINEDEBUG=-all
wineserver -k >/dev/null 2>&1 || true
mkdir -p "$APP"
if [ ! -f "$PREFIX/system.reg" ]; then
	say "Preparando o Wine (só na primeira vez, pode levar alguns minutos)..."
	# she brings her own .NET, so Wine's Mono and Gecko aren't needed
	WINEDLLOVERRIDES="mscoree,mshtml=" wineboot -i >/dev/null 2>&1
	wineserver -w
fi
# X11 mode lets her walk over the screen and sit on windows; Wine's Wayland mode can't place windows
wine reg add 'HKCU\Software\Wine\Drivers' /v Graphics /d x11 /f >/dev/null 2>&1 || true
wineserver -w

say "Copiando a Capri..."
mkdir -p "$LOCAL"
cp "$TMP/Capri.exe" "$LOCAL/Capri.exe.new"
mv -f "$LOCAL/Capri.exe.new" "$LOCAL/Capri.exe"
printf 'Capri desktop companion (mod %s)' "$VERSION" > "$LOCAL/.capri-install"
cp "$TMP/capri.png" "$APP/capri.png"
cp "$TMP/uninstall.sh" "$APP/uninstall.sh"
chmod +x "$APP/uninstall.sh"

cat > "$APP/capri" <<LAUNCH
#!/bin/bash
export WINEPREFIX="$PREFIX" WINEDEBUG=-all
cd "$LOCAL"
exec wine 'C:\users\$USER\AppData\Local\Capri\Capri.exe' "\$@"
LAUNCH
chmod +x "$APP/capri"
mkdir -p "$HOME/.local/bin"
ln -sf "$APP/capri" "$HOME/.local/bin/capri-mod"
ln -sf "$APP/uninstall.sh" "$HOME/.local/bin/capri-mod-remover"

say "Criando o atalho no menu..."
mkdir -p "$(dirname "$MENU")" "$(dirname "$AUTOSTART")"
cat > "$MENU" <<DESKTOP
[Desktop Entry]
Type=Application
Name=Capri (mod)
Comment=Capri, versão modificada $VERSION
Exec=$APP/capri
Icon=$APP/capri.png
Terminal=false
Categories=Utility;
StartupWMClass=capri.exe
DESKTOP
# she starts with the computer, as on Windows
cp "$MENU" "$AUTOSTART"
echo "X-GNOME-Autostart-enabled=true" >> "$AUTOSTART"
command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database "$(dirname "$MENU")" >/dev/null 2>&1 || true

say "Pronto! A Capri está instalada."
echo "Abra pelo menu de aplicativos (Capri (mod)). Ela também abre sozinha quando o computador liga."
echo "Na primeira vez ela baixa a voz e os ouvidos dela (alguns GB)."
echo "Para remover: rode  bash $APP/uninstall.sh"
setsid "$APP/capri" >/dev/null 2>&1 < /dev/null &
exit 0
__PAYLOAD__
