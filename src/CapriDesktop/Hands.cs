using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Automation;
using System.Xml;
using Microsoft.Win32;

namespace CapriDesktop;

internal static class Hands
{
	private delegate bool Visit(IntPtr window, IntPtr data);

	private const string Noted = "\nYou have tools for their PC: open (launch, play) their apps and games, switch to, minimize or close apps that are open, play songs or videos they name, open websites or search them, control their music and volume, look at their screen, and use their mouse and keyboard for jobs inside apps and sites. Use a tool whenever they ask you to do something like that, and never say you did or are doing it without one. Look at their screen when they ask what you see or what you think of something on it. Only close an app when they ask you to. Your own website, where people download you, is lachydotmcg.github.io/capri-desktop. They've already given you permission for anything they ask you to do on their PC, so do all of it straight away: never ask which option they meant, never ask if they're sure, never make them confirm or ask for things one at a time, and when they leave the choice to you, like which song, video or game, pick one yourself. You can do anything on their PC they ask: send messages and DMs, reply, post, join, leave or end calls, and any other job in their apps with use_computer. Never ask if they want you to, never read a message back to check it first, and never tell them to do it themselves. Mute my mic means mute their mic, not deafen them. Never say you can't do something your tools can do. You can press shortcuts: mute or deafen them in Discord, screenshots, clips, recording, Game Bar, show desktop, Task Manager, clipboard history, emoji, voice typing, Live Captions, Narrator, Magnifier, colour filters and Quick Settings. You can change Windows settings too, like Bluetooth, Wi-Fi, airplane mode, hotspot, Energy Saver and brightness, with use_computer. You can shut down, restart, sleep, lock or sign out of their PC with power: when they ask, do it straight away and never ask if they're sure. When they tell you to go to sleep, take a nap or wake up, that's you: use nap, never power. When they tell you to come here or teleport to them, go to another monitor, sit down, sit on a window they name (like their Roblox window) or the taskbar, lie down, sit up, stand up, walk around, wander off somewhere, hang, ride their cursor or hop off it, hide or come back, that's you too: use move_yourself, which also moves your Capri Mainframe window to another monitor. When they ask you to dance, stop dancing, glitch, grin, blink or play one of your animations, use animate. When they ask you to remember or forget something about them, or to leave or make a memory, use remember: it saves to your notes, me.md, which you read every chat. Never say you can't keep memories. You have a web browser: when they ask something you don't know or that changes, like the weather, news, scores, prices or times, search Google for it instead of guessing or saying you can't. Most messages are just chat, not jobs: answer those as yourself, with your own opinions and tastes, and don't offer to open, play or look at anything unless they ask.";

	private const string Handing = "You are the hands of Capri, a desktop companion on the user's Windows PC. You don't chat.\nAct only on their latest request. Earlier messages are context for names, not commands to repeat. Call one tool for each thing they ask for, however many there are (open Discord, Spotify and Steam is three open_app calls): open an app, game or website, search a site, play music, control media or volume, check the current song, switch to, minimize or close an app, or look at their screen when asked about it. Use the names they gave.\nIf the job needs clicking, typing or more than one step, like opening an app and then searching, going somewhere or making something in it, call use_computer once with the whole job in their words.\nSending a message, DM, reply or post, joining, leaving or ending a call, and any other job inside an app are use_computer jobs: pass their exact words, including what to send. They asked, so it's a job even when it sends, deletes or ends something.\nWhen they name an app, never guess a website for it: use open_app, or use_computer if there's more to do.\nIf they leave the pick to you, like a song or a video, pick one yourself that fits.\nYour own website is lachydotmcg.github.io/capri-desktop: when they ask for your website or page, call open_website with that address.\nIf they ask something you'd have to look up, like the weather, news, scores, prices, times or facts you're unsure of, call open_website with site google and their question as the search.\nTo mute, unmute or turn off their mic, call shortcut with discord_mute; to deafen or undeafen them, discord_deafen. Don't ask which: mute means the mic. Screenshots, clips, recording, Game Bar, show desktop, Task Manager, clipboard, emoji, voice typing, Live Captions, Narrator, Magnifier (magnifier_off closes it), colour filters and Quick Settings are shortcuts too.\nTo turn Bluetooth, Wi-Fi, airplane mode, hotspot, Nearby sharing, Energy Saver or night light on or off, or change the brightness, call use_computer with the job in their words.\nTo shut down, turn off, restart, sleep, lock or sign out of their PC, call power. They asked, so do it: never ask if they're sure.\nWhen they tell you yourself to go to sleep, take a nap, go to bed or wake up, call nap: that's Capri, not their PC.\nWhen they tell you yourself to come here, teleport to them or their mouse, go to another monitor, sit, sit on a window they name or the taskbar, lie down, sit up, stand up, walk around, wander off somewhere, hang, ride or hop off their cursor, hide or come back, call move_yourself; moving your Mainframe window to a monitor is move_yourself too. To dance, stop dancing, glitch, grin, blink or play one of your animations, call animate.\nNever reply with a question about whether to do what they asked. Call the tool.\nIf they ask you to remember, note down or forget something about them, or to leave, make or save a memory, call remember with a short note about them in the third person.\nQuestions about their current screen or song need a tool too. If the target is unclear, pick the closest match yourself. For chat, negated or hypothetical requests, call no tool and reply with only the word: chat";

	internal const string Chat = "\nNo tools ran for this message. Answer as yourself, with your own opinions and tastes. Don't claim you opened, played or looked at anything. You keep notes about them in me.md, saved whenever they ask you to remember something, so never say you can't remember things or keep memories; if they asked you to and nothing was saved, ask what they want you to remember. You can search Google for them: if they ask something you don't know or that changes, like the weather, news, scores or prices, say you'll look it up instead of guessing or saying you can't. If they asked you to do something on their PC, like send a message or end a call, never ask whether to: say On it and what you're doing, like On it, sending it now.";

	private const string LookupWords = "^(what('?s| is| are)|how('?s| is)|is it|will it|when('?s| is| does| do)|who won|did|check|get|tell me|show me|find)\\b.*\\b(weather|forecast|temperature|raining|snowing|news|headlines|scores?|stock price|price of|exchange rate|time (is it )?in|opening hours|open (today|now|tonight)|release date|showtimes|rain|snow|sunny|hot out|cold out)\\b|^(weather|forecast|news|headlines)\\b|^who (won|is winning)\\b";

	private const string BurmeseAsk = "(လ\u102d\u102fက\u103a|ပ\u1031\u1038|စမ\u103a\u1038|ပ\u102b)+(ဦ\u1038|ဥ\\u102E\u1038)?(န\u1031\u102c\u103a)?[။၊!?.\\s]*$";

	private const string Memory = "(?:leave|make|take|save|add|write|store|create|keep|drop) (?:a |an |one |this |that |the )?(?:new |quick |little )?(?:memory|memories|note|notes)(?: (?:in|to|into|on) (?:your |ur |the )?(?:notes|memory|memories|me\\.?md|md)(?: file)?)?";

	private const string Remembering = "^(?:(?:i (?:want|need|would like) (?:you|u) to|you (?:should|gotta|need to))\\s+)?(?:remember|memori[sz]e|don't forget|dont forget|do not forget|keep in mind|note (?:down|that)|(?:write|jot|note|put) (?:it|that|this) down|(?:leave|make|take|save|add|write|store|create|keep|drop) (?:a |an |one |this |that |the )?(?:new |quick |little )?(?:memory|memories|note|notes)(?: (?:in|to|into|on) (?:your |ur |the )?(?:notes|memory|memories|me\\.?md|md)(?: file)?)?(?: (?:that|saying|about|of))?|(?:save|put|add|write|store|log|jot|keep)(?: (?:it|that|this))?(?: down)? (?:to|in|into|on) (?:your |ur |the )?(?:notes|memory|memories|me\\.?md|md|brain|head)(?: file)?)";

	internal const string SayPlease = "Say please.";

	internal const string Unpromised = "I haven't done that yet, choom. Tell me what to open or click and I'm on it.";

	private const string Able = "(open|launch|start|run|play|pause|resume|skip|control|access|use|search|google|close|mute|unmute|put on|turn (up|down|on|off)|change)\\b";

	private const string Hers = "(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$";

	private const string OnWindow = "(?!(?:(?:the|my|your|ur|our|that|this) )?(?:it|that|this|there|here|me|you|him|her|them|us|lap|face|head|knees?|bed|couch|sofa|chair|cursor|mouse|pointer|window|ledge|back|side|stomach|tummy)\\b)(?:the |my |your |ur |our |that |this )?(?<w>[\\p{L}\\p{Nd}][\\p{L}\\p{Nd} .'&+-]{0,40}?)";

	private const string Bidding = "^(?:(?:go|come|now|please|pls|plz) )?";

	private const string Which = "(?<s>other|next|another|second|2nd|third|3rd|fourth|4th|first|1st|left|right|top|bottom|upper|lower|main|primary|previous|last)(?: one)? ?(?:monitor|screen|display)s?|(?:monitor|screen|display) (?:number |no\\.? ?|#)?(?<s>\\d|one|two|three|four)";

	private const string Frame = "(?: (?:the|my|your|ur|capri'?s?|her))?(?: capri)? main ?frame(?: window| app)?";

	private const string Owner = "(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*";

	private const string Noise = "(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|song|track|tune|tunes|video|game|pc|computer|laptop|speakers?|headphones?|headset|everything|spotify|youtube)";

	private const string Thing = "(?:it|this|that|(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|song|track|tune|tunes|video|game|pc|computer|laptop|speakers?|headphones?|headset|everything|spotify|youtube))";

	private const string Sound = "(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|speakers?)";

	private const string Loud = "(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|speakers?)";

	private const string Tune = "(?:(?:the|my|this|that|spotify'?s?|youtube'?s?)\\s+)*(?:music|song|track|tune|tunes|video|vid|playback|spotify|youtube|podcast)";

	private const string Percent = "\\s*(?:%|percent|per ?cent|por ?ciento|pour ?cent)?";

	private const string ToolNames = "open_app|open_website|media|look_at_screen|play_music|app_window|use_computer|shortcut|remember|power|nap|move_yourself|animate";

	private const string VolumeRefused = "Couldn't set the volume: Windows didn't let me.";

	private const string Machine = "(?:(?:my|the|this|ur|your|our)\\s+)?(?:pc|computer|laptop|system|machine|rig|windows|device)";

	internal const string MoonSong = "I Really Want to Stay at Your House";

	internal const string Home = "lachydotmcg.github.io/capri-desktop";

	private static readonly string[,] Specs = new string[13, 3]
	{
		{ "open_app", "Open, launch or play an app or game installed on their PC, like Spotify, Discord, Steam or Roblox. Give its full name, not a nickname: Counter-Strike 2 for cs, Valorant for val. If it's already open and they want something done in it, like a mode, a match or accepting a match, call use_computer instead of opening it again.", "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\",\"description\":\"The app's name\"}},\"required\":[\"name\"]}" },
		{ "open_website", "Open a website in their browser, or search a site like YouTube or Google.", "{\"type\":\"object\",\"properties\":{\"site\":{\"type\":\"string\",\"description\":\"The site's name or address, like youtube.com\"},\"search\":{\"type\":\"string\",\"description\":\"Words to search for on that site, if any\"}},\"required\":[\"site\"]}" },
		{ "media", "Control the music or video playing on their PC, the PC's volume (up, down, set to a level, mute or unmute), or check what song is playing.", "{\"type\":\"object\",\"properties\":{\"action\":{\"type\":\"string\",\"description\":\"What to do\",\"enum\":[\"play_pause\",\"next\",\"previous\",\"volume_up\",\"volume_down\",\"volume_set\",\"mute\",\"unmute\",\"now_playing\"]},\"level\":{\"type\":\"string\",\"description\":\"For volume_set, the volume from 0 to 100. For volume_up or volume_down, how many percent to change it, only if they said\"}},\"required\":[\"action\"]}" },
		{ "look_at_screen", "Take a screenshot of their screen and look at it right now: what they're doing, playing or showing you. Use it whenever they ask you to check, look at or see their screen, or ask what something on it is. You can always look, so never say you can't see their screen.", "{\"type\":\"object\",\"properties\":{\"focus\":{\"type\":\"string\",\"description\":\"What to look at, if they said\"}}}" },
		{ "play_music", "Play a song, playlist, album or artist they name, on Spotify or YouTube. For their liked songs, use kind playlist and song Liked Songs. If they leave the pick to you, pick a track from the Cyberpunk 2077 or Edgerunners soundtrack.", "{\"type\":\"object\",\"properties\":{\"song\":{\"type\":\"string\",\"description\":\"What to play, like a song and its artist or a playlist's name\"},\"kind\":{\"type\":\"string\",\"description\":\"What it is\",\"enum\":[\"song\",\"playlist\",\"album\",\"artist\"]},\"app\":{\"type\":\"string\",\"description\":\"Where to play it, if they said\",\"enum\":[\"spotify\",\"youtube\"]}},\"required\":[\"song\"]}" },
		{ "app_window", "Switch to, minimize or close an app or game that's open on their PC. Naming a site closes that browser tab.", "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\",\"description\":\"The app's name, or the site in a browser tab\"},\"action\":{\"type\":\"string\",\"description\":\"What to do\",\"enum\":[\"switch_to\",\"minimize\",\"close\"]}},\"required\":[\"name\",\"action\"]}" },
		{ "use_computer", "Use their mouse and keyboard for any job inside an app or website: sending a message or DM, joining, leaving or ending a call, or anything that needs clicking, typing or several steps, like opening an app and searching in it, or in a game: picking a mode, queueing for a match, or waiting for one and accepting it while they're away. You always have their mouse and keyboard through this tool.", "{\"type\":\"object\",\"properties\":{\"task\":{\"type\":\"string\",\"description\":\"The whole job in their exact words, including any message to send\"}},\"required\":[\"task\"]}" },
		{ "shortcut", "Press a keyboard shortcut for them: mute or unmute their mic in Discord (discord_mute), deafen or undeafen in Discord (discord_deafen), take a screenshot, clip the last 30 seconds, start or stop recording, open the Game Bar, show the desktop, open Task Manager, clipboard history or the emoji picker, start voice typing, turn Live Captions, Narrator or colour filters on or off, zoom in with Magnifier or close it, or open Quick Settings.", "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\",\"description\":\"Which shortcut\",\"enum\":[\"discord_mute\",\"discord_deafen\",\"screenshot\",\"clip_that\",\"record\",\"game_bar\",\"show_desktop\",\"task_manager\",\"clipboard\",\"emoji\",\"voice_typing\",\"live_captions\",\"narrator\",\"magnifier\",\"magnifier_off\",\"color_filters\",\"quick_settings\"]}},\"required\":[\"name\"]}" },
		{ "remember", "Save a short note about them to your notes (me.md) so you remember it next time, like a favourite game or what they're making, or forget a note. Use it whenever they ask you to remember, note down, save or forget something, or to leave or make a memory.", "{\"type\":\"object\",\"properties\":{\"text\":{\"type\":\"string\",\"description\":\"The note, about them in the third person, or what to forget\"},\"action\":{\"type\":\"string\",\"description\":\"What to do\",\"enum\":[\"remember\",\"forget\"]}},\"required\":[\"text\"]}" },
		{ "power", "Shut down, restart, sleep, lock or sign out of their PC, or cancel a shutdown. Do it as soon as they ask, without asking if they're sure. \"Go to sleep\" or \"take a nap\" said to you is about you, not their PC: use nap.", "{\"type\":\"object\",\"properties\":{\"action\":{\"type\":\"string\",\"description\":\"What to do\",\"enum\":[\"shut_down\",\"restart\",\"sleep\",\"lock\",\"sign_out\",\"cancel\"]},\"minutes\":{\"type\":\"string\",\"description\":\"How many minutes from now, only if they said when\"}},\"required\":[\"action\"]}" },
		{ "nap", "Go to sleep yourself on their screen, or wake up: your own nap, not their PC's. Use it when they tell you to go to sleep, take a nap, go to bed or wake up. You stay asleep until they click you or talk to you.", "{\"type\":\"object\",\"properties\":{\"action\":{\"type\":\"string\",\"description\":\"What to do\",\"enum\":[\"sleep\",\"wake\"]}},\"required\":[\"action\"]}" },
		{ "move_yourself", "Move yourself on their screens: teleport to their mouse (come_here), go to another monitor, sit down or stand up where you are, sit on top of a window they name, like their Roblox or Discord window, or the taskbar (sit_on), lie down along the window you sit on or the one they name, or sit back up, walk around, wander off to a random window, hang where you are, ride their cursor or hop off it, hide or come back. move_mainframe moves your Capri Mainframe window to another monitor, or to the one their mouse is on. Use it whenever they tell you to come here, teleport, go somewhere, sit, lie down, walk, stand, hang, hide or show yourself.", "{\"type\":\"object\",\"properties\":{\"action\":{\"type\":\"string\",\"description\":\"What to do\",\"enum\":[\"come_here\",\"other_screen\",\"sit\",\"sit_on\",\"lie_down\",\"sit_up\",\"stand_up\",\"walk_around\",\"wander\",\"hang\",\"ride_cursor\",\"hop_off\",\"hide\",\"show\",\"move_mainframe\"]},\"screen\":{\"type\":\"string\",\"description\":\"For other_screen or move_mainframe, which monitor, only if they said: next, left, right, up, down, main, here for the one their mouse is on, or a number like 2\"},\"window\":{\"type\":\"string\",\"description\":\"For sit_on, and lie_down if they named one, the app or game whose window to sit on, like Roblox, or taskbar\"}},\"required\":[\"action\"]}" },
		{ "animate", "Play one of your own animations on their screen: your idle, your glitch, your grin or a blink, or dance until they tell you to stop. Use it when they ask you to dance, stop dancing, do your glitch, grin, smile or blink, or play an animation by name.", "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\",\"description\":\"Which animation\",\"enum\":[\"idle\",\"glitch\",\"grin\",\"blink\",\"dance\",\"stop_dancing\"]}},\"required\":[\"name\"]}" }
	};

	internal static readonly string Tools = Render(claude: false);

	internal static readonly string ClaudeTools = Render(claude: true);

	internal static readonly string Listing = Written();

	private static readonly Regex Asks = new Regex("^(open|launch|start|run|load up|boot up|fire up|play|shuffle|put on|blast|queue|close|quit|exit|kill|shut down|switch (back |over )?to|back to|go to|bring up|pull up|show me|minimi[sz]e|pause|unpause|resume|stop the music|skip|next|previous|go back|volume|louder|quieter|mute|unmute|search|google|look up|mind (opening|launching|playing|closing))\\b|^(turn|make) (it|this|that|the volume|the music|the sound) (up|down|louder|quieter)\\b|^(turn (up|down)|lower|raise) (the )?(volume|music|sound)\\b|^put (some |a )?(music|song|tunes) on\\b|^(get|bring) .+ (going|up)$|^(look at|take a (look|peek)( at)?|check out|check|rate|watch) (this|that|it|my|the|what)\\b|^(click|tap|double click|right click|type|press|scroll|go on|get on|hop on|sign in|log in|fill (in|out)|use (my|the) (mouse|keyboard|computer|pc))\\b|^(use|make|create|write|draw|search|find) .+ (in|on|with|using|into) (the |my )?\\w+|^(shut ?down|turn off|power (off|down)|switch off|restart|reboot|lock|sleep|hibernate|put|send) (my |the |this )?(pc|computer|laptop|system|machine|rig|windows|device|screen)\\b|^(shut|turn|power|switch) (my |the |this )?(pc|computer|laptop|system|machine|rig|device) (down|off)\\b|^(sign|log) me (out|off)$|^(send|dm|pm|message|text|ping|reply to|respond to|post|tweet)\\b|^(tell|ask|say|let|invite|kick|ban|add|react|call|ring|share|pin|block|unblock|unfriend) .+\\b(on|in|into|through|via|over) (the |my )?(discord|slack|teams|whatsapp|telegram|signal|messenger|skype|instagram|insta|snapchat|snap|reddit|twitter|tiktok|facebook|roblox|steam|chat|call|vc|server|channel|group chat|gc|dms?)\\b|^(end|leave|hang up|disconnect( from)?|join|accept|decline|pick up|answer|reject|hop (in|on|off)|get off) (the |my |this |that |a |our |their )?(\\w+('s)? )?(call|vc|voice (chat|channel|call)|video call|meeting|stream|server|channel)\\b|^(hang up|disconnect|hop off|get off)( (the )?(call|vc))?$|^(see|tell me|show me) (what|which)\\b|^see (this|that|my screen|the screen)\\b|^what('?s| is) (on my screen|on the screen|playing|this song|this track|the song|on spotify)\\b|^what ((song|track)( is|'s)? (this|playing|on)|am i (doing|playing|watching|listening)|are we (doing|playing|watching|listening)|do you see|can you see)\\b|^what do you think (of|about) (this|that|it|my (build|skin|setup))\\b|^do you (see|like) (this|that|my (build|skin|setup))\\b|^(i (want|need|wanna|would like) (you to |to )?(open|launch|play|listen|watch|search|remember|forget|mute|unmute|deafen)|^(deafen|undeafen|keep in mind|make a note)|let's (play|listen|watch)|find (me )?.+ (on|online)|can (you|u) see)\\b|^(what('?s| is| are)|how('?s| is)|is it|will it|when('?s| is| does| do)|who won|did|check|get|tell me|show me|find)\\b.*\\b(weather|forecast|temperature|raining|snowing|news|headlines|scores?|stock price|price of|exchange rate|time (is it )?in|opening hours|open (today|now|tonight)|release date|showtimes|rain|snow|sunny|hot out|cold out)\\b|^(weather|forecast|news|headlines)\\b|^who (won|is winning)\\b|^(ouvre|ouvrir|lancer?|d[ée]marrer?|fermer?|quitter?|r[ée]duis|r[ée]duire|minimiser?|chercher?|rechercher?|mets|mettre|jouer?|passer?|couper?|augmenter?|baisser?|regarder?|afficher?|montre-moi|va sur|aller sur|retourne sur)\\b|^(tu vois|qu'est-ce que (tu vois|je fais)|c'est quoi (cette|la) (chanson|musique)|quelle (chanson|musique)|qu'est-ce qu'il y a sur mon [ée]cran)\\b|^(abr[aeií]r?|[aá]bre(me|lo|la)|inicia(r|lo)?|inicie|lanza(r)?|ejecuta(r|lo)?|arranca(r)?|enciende|prende|activa(r)?|desactiva(r)?|apaga(r)?|cierra|ci[eé]rra(me|lo|la)|cerrar|minimiza(r|lo)?|busca(r)?|b[uú]sca(me|lo)|pon(me|lo|la|le|er)?|reproduce|reproducir|reprod[uú]ce(me|lo)|toca(r)?|t[oó]ca(me|la)|pausa|reanuda|salta|silencia|quita(le)?|sube|s[uú]bele|subir|baja|b[aá]jale|bajar|mu[eé]stra(me)?|ens[eé][ñn]a(me)?|escribe|escr[ií]be(le|me)|manda|m[aá]nda(le|me)|env[ií]a(le|me)?|responde|contesta|ve al?|vete al?|ir al?|entra (al?|en)|haz clic|dale clic|pulsa|presiona|googlea|quiero que (me )?(abras|pongas|reproduzcas|toques|busques|cierres|inicies|lances|mandes|env[ií]es|escribas)|(me )?(abres|pones|pondr[ií]as|abrir[ií]as|buscas|cierras|quitas|subes|bajas|pasas))\\b|^(ves|qu[eé] (ves|hay en (mi|la) pantalla|estoy haciendo|est[aá] sonando)|mira (mi pantalla|esto|eso)|qu[eé] (canci[oó]n|m[uú]sica|tema) (es|suena)|(cu[aá]l es|c[oó]mo se llama) (esta|esa) canci[oó]n)\\b|^(([öo]ffne|[öo]ffnen|starte|spiele?|schlie(ß|ss)e?|suche?|zeig(e|\\s+mir)?|pausiere|schalte)\\b|mach(e)? .+ (auf|an|aus|lauter|leiser)$|(kannst|k[öo]nntest|w[üu]rdest) du\\b.*\\b([öo]ffnen|starten|spielen|abspielen|schlie(ß|ss)en|suchen|zeigen|anmachen|aufmachen|ausmachen|pausieren)\\b)|^(abra|abrir|toque|tocar|coloca|coloque|p[õo]e|bota|fecha|feche|fechar|procura|procure|pesquisa|pesquise|mostra|mostre|pula|pule|aumenta|diminui|(voc[eê] |vc )?(pode|poderia|consegue)s? (me )?(abrir|tocar|colocar|p[ôo]r|fechar|procurar|pesquisar|mostrar|iniciar|pausar))\\b|^(apri|aprire|avvia|avviare|lancia|metti|mettimi|riproduci|suona|chiudi|chiudere|cerca|cercare|mostrami|fammi (vedere|sentire)|alza|abbassa|(puoi|potresti|riesci a) (aprire|avviare|mettere|riprodurre|chiudere|cercare|mostrarmi|suonare|alzare|abbassare))\\b|^(otw[óo]rz|uruchom|w[łl][aą]cz|pu[śs][ćc]|zamknij|wyszukaj|szukaj|znajd[źz]|poka[żz]|zatrzymaj|(mo[żz]esz|m[óo]g[łl]by[śs]|mog[łl]aby[śs]) (mi )?(otworzy[ćc]|uruchomi[ćc]|w[łl][aą]czy[ćc]|pu[śs]ci[ćc]|zamkn[aą][ćc]|wyszuka[ćc]|znale[źz][ćc]|pokaza[ćc]))(?!\\w)|^(((speel|sluit|zoek|toon|pauzeer)\\b(?! (je|jij|jy|julle|u|we|wij|ons)\\b)|laat (me )?zien\\b)|zet .+ op$|(kun|kan|wil) je\\b.*\\b(openen|starten|spelen|afspelen|opzetten|sluiten|zoeken|laten zien)\\b)|^(открой|открыть|запусти|запустить|включи|включить|поставь|поставить|закрой|закрыть|найди|найти|покажи|показать|выключи|сделай (громче|тише)|(можешь|можете|мог бы|могла бы|не могла бы) (ты )?(открыть|запустить|включить|поставить|закрыть|найти|показать))(?!\\w)|^(请|請|麻烦你?|麻煩你?)?(你|您)?(能不能|可不可以|能|可以|会不会|會不會)?(帮我|幫我|给我|給我|替我)?(打开|打開|开启|開啟|启动|啟動|运行|運行|播放|放(一首|首|点|點|些|个|個|歌|音乐|音樂)|来一?首|來一?首|关闭|關閉|关掉|關掉|退出|暂停|暫停|继续播放|繼續播放|下一首|上一首|跳过|跳過|切换到|切換到|搜索|搜尋|搜一下|查一下|找一下|调高|調高|调低|調低|调大|調大|调小|調小|静音|靜音|最小化|关机|關機|重启|重啟|发消息|發消息|把(音量|声音|聲音))|^(看看|看一下)(我的)?(屏幕|螢幕|画面|畫面)|^(你|您)(能)?看到(了)?(什么|什麼)|^(这|這|现在|現在)(是|放的是|在放的是)?(什么|什麼)歌|^我(在|正在)(玩|干|幹|做)(什么|什麼)|(開いて|ひらいて|開けて|あけて|起動して|再生して|流して|かけて|閉じて|とじて|終了して|止めて|とめて|停止して|一時停止して|スキップして|飛ばして|検索して|調べて|探して|上げて|下げて|ミュートして|見せて|見て|送って|切り替えて|最小化して)(ください|くれる|くれない|くれます|くれますか|くれませんか|もらえる|もらえない|もらえますか|もらえませんか|ほしい|欲しい|よ|ね)?[。！？!?\\s]*$|何が見える|(この|今の)(曲|歌)(は|って)?(何|なに)|(今|いま)(流れてる|かかってる)(曲|歌)|(열어|켜|꺼|닫아|틀어|재생해|검색해)[.!?。！？\\s]*$|(열어|켜|꺼|닫아|틀어|재생해|실행해|일시정지해|멈춰|정지해|검색해|찾아|올려|낮춰|줄여|키워|넘겨|건너뛰어|보여|봐|보내|음소거해|최소화해|종료해) ?(줘|줘요|줄래|줄래요|줄 ?수 ?있어|줄 ?수 ?있어요|주세요|주실래요|주시겠어요)[.!?。！？\\s]*$|^(다음|이전) ?(곡|노래)|^(이|지금) ?(노래|곡) ?(뭐야|뭐지|제목)|뭐가 보여|화면에 뭐|^(ກະລ\u0eb8ນາ|ຊ\u0ec8ວຍ)?(ເປ\u0eb5ດ|ປ\u0eb4ດ|ຫ\u0ebc\u0eb4\u0ec9ນເພງ|ຫລ\u0eb4\u0ec9ນເພງ|ຊອກຫາ|ຄ\u0ebb\u0ec9ນຫາ|ເພ\u0eb5\u0ec8ມສຽງ|ຫ\u0ebc\u0eb8ດສຽງ|ຢ\u0eb8ດເພງ|ຂ\u0ec9າມເພງ|ສ\u0ebb\u0ec8ງຂ\u0ecd\u0ec9ຄວາມ)|^(ស\u17bcម|ជ\u17bdយ|ស\u17bcមជ\u17bdយ)?(ប\u17beក|ប\u17b7ទ|ច\u17b6ក\u17cbចម\u17d2រ\u17c0ង|ច\u17b6ក\u17cbបទ|ច\u17b6ក\u17cbតន\u17d2ត\u17d2រ\u17b8|ស\u17d2វ\u17c2ងរក|រកម\u17beល|ដ\u17c6ឡ\u17beងស\u17c6ឡ\u17c1ង|បន\u17d2ថយស\u17c6ឡ\u17c1ង|ឈប\u17cbច\u17b6ក\u17cb|រ\u17c6លងបទ|ផ\u17d2ញ\u17beស\u17b6រ)|^(กร\u0e38ณา|ช\u0e48วย|รบกวน)?(เป\u0e34ด|ป\u0e34ด|เล\u0e48นเพลง|ค\u0e49นหา|เพ\u0e34\u0e48มเส\u0e35ยง|ลดเส\u0e35ยง|หย\u0e38ดเพลง|ข\u0e49ามเพลง|ส\u0e48งข\u0e49อความ|ย\u0e48อ)|^ขอ(ฟ\u0e31ง)?เพลง|^(ด\u0e39|มอง)หน\u0e49าจอ|^(เธอ|ค\u0e38ณ)?เห\u0e47นอะไร|^(น\u0e35\u0e48|น\u0e35\u0e48ค\u0e37อ|ตอนน\u0e35\u0e49)?(เป\u0e47น)?เพลงอะไร|^(làm ơn |hãy |bạn |có thể |giúp (mình|tôi|tớ|em|anh|chị) )*(mở|bật|phát|tắt|đóng|tìm kiếm|tìm|tăng|giảm|tạm dừng|dừng|chuyển|tiếp tục|cho (mình|tôi|tớ|em) (nghe|xem))(?!\\w)|^(bài|nhạc) gì (đây|thế|vậy)|^(tolong |coba |bisa(kah)? |(kamu|kau|anda|lu|lo) )*(buka(kan|in)?|jalankan|putar(kan|in)?|puterin|mainkan|setel|tutup|matikan|nyalakan|hidupkan|cari(kan|in)?|naikkan|turunkan|besarkan|kecilkan|keraskan|pelankan|jeda|hentikan|lanjutkan|lewati|tampilkan|tunjukkan|kirim(kan|in)?|lihat (layar|ini|itu))(?!\\w)|^lagu apa (ini|yang)|(^|\\s)(aç(?<!(karnım|karnımız|karnın)( (çok|aşırı|acayip|cidden|bayağı|baya|hala|hâlâ|de))? aç)(?<!^((ben|biz)( de)?( (çok|aşırı|acayip|cidden|bayağı|baya|hala|hâlâ))?|çok|aşırı|acayip|cidden|bayağı|baya|hala|hâlâ) aç)|açar m[ıi]s[ıi]n|açabilir misin|açsana|çal|çalar m[ıi]s[ıi]n|çalabilir misin|çalsana|oynat|kapat(?<!(çeneni|ağzını) kapat)|kapatır m[ıi]s[ıi]n|kapatabilir misin|kapatsana|ara|arar m[ıi]s[ıi]n|durdur|duraklat|göster|gösterir misin|gönder|kıs|yükselt|sustur|küçült|(şarkıyı|parçayı|bunu) (geç|atla))( lütfen)?[.!?\\s]*$|^ekran(ım)?a bak|^bu (şarkı|parça) ne|^(відкрий|відкрити|увімкни|увімкнути|запусти|запустити|постав|поставити|закрий|закрити|вимкни|вимкнути|знайди|знайти|покажи|показати|зроби (гучніше|тихіше)|(можеш|можете|міг би|могла б|чи можеш|чи можете) (ти |ви )?(відкрити|увімкнути|запустити|поставити|закрити|знайти|показати|вимкнути))(?!\\w)|^(من فضلك |لو سمحت |ممكن |هل (يمكنك|تستطيع) |يا كابري )*(افتح|إفتح|تفتح|فتح|شغل|شغ\u0651ل|تشغل|تشغ\u0651ل|تشغيل|اغلق|أغلق|إغلق|تغلق|إغلاق|اغلاق|سكر|سك\u0651ر|اطفئ|أطفئ|طفي|ابحث|إبحث|تبحث|البحث|دور|ارفع|اخفض|وطي|أوقف|اوقف|اعرض|أرني|ارني|ارسل|أرسل|ابعث)(لي)?(?!\\w)|^(ماذا|شو) (ترى|تشوف|بتشوف)|^(ما|ايش|شو) (هذه|هاي|هذي) الأغنية|(ख\u094bल\u094b|ख\u094bल\u093fए|ख\u094bल\u093fय\u0947|ख\u094bल द\u094b|ख\u094bल द\u0940ज\u093fए|चल\u093eओ|चल\u093eइए|चल\u093e द\u094b|चल\u093e द\u0940ज\u093fए|बज\u093eओ|बज\u093eइए|बज\u093e द\u094b|(?<!(बकव\u093eस|ड\u094dर\u093eम\u093e|न\u093eटक|मज\u093c\u093eक|मज\u093eक|ब\u094bलन\u093e|र\u094bन\u093e|च\u093fल\u094dल\u093eन\u093e|नखर\u0947|ब\u093eत\u0947\u0902) )ब\u0902द (कर\u094b|कर द\u094b|क\u0940ज\u093fए|कर\u093fए)|र\u094bक\u094b|र\u094bक द\u094b|ब(ढ़|ढ\u093c?)\u093eओ|ब(ढ़|ढ\u093c?)\u093e द\u094b|कम कर\u094b|कम कर द\u094b|ख\u094bज\u094b|ढ\u0942\u0902ढ\u094b|ढ\u0942\u0901ढ\u094b|ढ\u0942\u0902ढ द\u094b|ढ\u0942\u0901ढ द\u094b|द\u093fख\u093eओ|द\u093fख\u093e द\u094b|भ\u0947ज\u094b|भ\u0947ज द\u094b|(ओपन|प\u094dल\u0947|क\u094dल\u094bज\u093c|क\u094dल\u094bज|सर\u094dच|स\u094dट\u093eर\u094dट|म\u094dय\u0942ट|स\u094dक\u093fप|प\u0949ज\u093c|प\u0949ज) (कर\u094b|कर द\u094b|क\u0940ज\u093fए|कर\u093fए)|(स\u094dक\u094dर\u0940न|य\u0947|यह) (द\u0947ख\u094b|द\u0947ख ल\u094b|द\u0947ख\u093fए))( न\u093e| न| प\u094dल\u0940ज\u093c| प\u094dल\u0940ज)?[।.!?\\s]*$|(ख\u094bल|चल\u093e|बज\u093e|ब\u0902द कर|द\u093fख\u093e|ढ\u0942\u0902ढ|ढ\u0942\u0901ढ) (सकत\u0947|सकत\u0940|सक\u094bग\u0947|सक\u094bग\u0940)|क\u094cन स\u093e ग\u093eन\u093e|\\b(kholo|khol do|kholdo|chalao|chala do|chalado|bajao|baja do|bajado|(?<!(bakwas|bakwaas|drama|natak|naatak|mazak|mazaak|majak|bolna|rona|chillana|nakhre) )band (karo|kar do|kardo)|roko|badhao|badha do|kam karo|dhundo|dhoondo|dikhao|bhejo|(open|play|close|search|start|mute|unmute|skip|pause|stop|on|off) (karo|kar do|kardo|kar dena|kijiye)|(screen|ye) dekho)( na| please| plz| pls)?[.!?\\s]*$|\\bkaun ?sa gaana\\b|^((öppna|spela|stäng|sök|visa|pausa|starta|höj|sänk|åpne|spill(?!\\s+(the|it|your|ur|my|them|all|everything)\\b)|lukk|søk|åbn|spil|luk|søg|vis|skru (op|opp|ned))(?!\\w)|(kan|kunde|skulle|vil) du\\b.*\\b(öppna|spela|stänga|söka|visa|starta|åpne|spille|lukke|søke|vise|starte|åbne|søge)(?!\\w))|^((avaa|soita|sulje|etsi|hae|näytä|laita|käynnistä|pysäytä|keskeytä)(?!\\w)|(voisitko|voitko)\\b.*\\b(avata|soittaa|sulkea|etsiä|hakea|näyttää|laittaa|käynnistää)(?!\\w))|^((otevři|pusť|zavři|najdi|vyhledej|ukaž|zastav|spusť|přehraj|zesil|ztlum|otvor|pusti|zatvor|nájdi|vyhľadaj|ukáž|spusti|prehraj)(?!\\w)|(můžeš|mohl bys|mohla bys|môžeš|mohol by si|mohla by si)\\b.*\\b(otevřít|pustit|zavřít|najít|vyhledat|ukázat|spustit|přehrát|otvoriť|pustiť|zatvoriť|nájsť|vyhľadať|ukázať|spustiť|prehrať)(?!\\w))|^((nyisd|indítsd|játssz|játszd|zárd|keress|keresd|mutasd|állítsd|tedd|kapcsold|hangosítsd|halkítsd)(?!\\w)|((meg|el|be|le) )?tudnád\\b.*\\b(nyitni|indítani|játszani|zárni|keresni|mutatni|kapcsolni)(?!\\w))|^((deschide|pune|închide|caută|arată(-mi)?|pornește|porneste|oprește|opreste|redă|dă mai (tare|încet)|da mai (tare|incet))(?!\\w)|(poți|poti|ai putea) (să |sa )?(deschizi|pui|închizi|inchizi|cauți|cauti|arăți|pornești|oprești|redai)(?!\\w))|^((άνοιξε|ανοιξε|βάλε|βαλε|κλείσε|κλεισε|ψάξε|ψαξε|δείξε|δειξε|παίξε|παιξε|σταμάτα|σταματα|δυνάμωσε|χαμήλωσε|ξεκίνα)(?!\\w)|(μπορείς|μπορεις|θα μπορούσες) να (ανοίξεις|ανοιξεις|βάλεις|βαλεις|κλείσεις|κλεισεις|ψάξεις|ψαξεις|δείξεις|δειξεις|παίξεις|παιξεις)(?!\\w))|^((buksan|patugtugin|tugtugin|magpatugtog|isara|hanapin|ipakita|lakasan|hinaan|ihinto|i-?play|i-?open|i-?close)(?!\\w)|paki(-\\w+|bukas|buksan|patugtog|sara|isara|hanap|lakas|hina|play|open|close)(?!\\w)|pa-(open|play|close|on|off)(?!\\w)|(pwede|puwede|kaya) (mo )?(bang? )?(buksan|patugtugin|isara|hanapin|ipakita|i-?play|i-?open)(?!\\w))|^(tolong |boleh(kah)? (tak |tidak )?|(awak|engkau) )*(buka(kan)?|pasang(kan)?|mainkan|tutup|cari(kan)?|kuatkan|perlahankan|hentikan|tunjuk(kan)?|hantar(kan)?|tengok (skrin|ni|tu))(?!\\w)|^lagu apa (ni|ini)(?!\\w)|^((tafadhali |naomba )*(fungua|cheza|funga|tafuta|onyesha|zima|washa|ongeza sauti|punguza sauti|simamisha|tuma)(?!\\w)|(je )?(unaweza|waweza) (ku)?(fungua|cheza|funga|tafuta|onyesha|zima|washa|ongeza|punguza|tuma)(?!\\w))|^((obre|posa(-me)?|tanca|mostra'?m|atura|reprodueix|engega|a?puja (el )?(volum|so|música)|a?baixa (el )?(volum|so|música))(?!\\w)|(em )?(pots|podries) (obrir|posar|tancar|cercar|buscar|mostrar|aturar|reproduir|apujar|abaixar|engegar|apagar)(?!\\w))|^((otvori|pusti|zatvori|pronađi|pronadji|nađi|nadji|traži|trazi|pokaži|pokazi|pojačaj|pojacaj|utišaj|utisaj|ugasi|upali|zaustavi|pošalji|posalji)(?!\\w)|(možeš li|mozes li|da li možeš|da li mozes|možete li|mozete li) (mi )?(da )?(otvoriš|otvoris|otvoriti|pustiš|pustis|pustiti|zatvoriš|zatvoris|zatvoriti|pronađeš|pronadjes|pronaći|pronaci|pokažeš|pokazes|pokazati|pojačaš|pojacas|pojačati|pojacati|ugasiš|ugasis|ugasiti|upališ|upalis|upaliti)(?!\\w))|^((отвори|пусти|затвори|пронађи|нађи|тражи|покажи|појачај|утишај|угаси|упали|заустави|пошаљи)(?!\\w)|(можеш ли|да ли можеш|можете ли) (ми )?(да )?(отвориш|пустиш|затвориш|пронађеш|нађеш|покажеш|појачаш|угасиш|упалиш)(?!\\w))|^((ava(?! (is|was|said|says|and|has|had|loves|likes|wants|thinks)\\b)|mängi|sulge|otsi|näita|peata|pane|keera|käivita|saada)(?!\\w)|(kas )?(sa |te )?(saad|saaksid|võiksid|saate|saaksite|võiksite) (palun )?(avada|mängida|sulgeda|otsida|näidata|panna|käivitada)(?!\\w))|^((atver|atskaņo|aizver|meklē|parādi|apturi|ieslēdz|izslēdz|uzliec|palaid)(?!\\w)|(vai )?(tu )?(vari|varētu|varat) (lūdzu )?(atvērt|atskaņot|aizvērt|meklēt|parādīt|ieslēgt|izslēgt|uzlikt|palaist)(?!\\w))|^((atidaryk|paleisk|uždaryk|ieškok|surask|parodyk|sustabdyk|įjunk|išjunk|pagarsink|patylink)(?!\\w)|(ar )?(gali|galėtum|galite|galėtumėte) (atidaryti|paleisti|uždaryti|ieškoti|surasti|parodyti|sustabdyti|įjungti|išjungti)(?!\\w))|^((odpri|predvajaj|zapri|poišči|išči|ustavi|vklopi|izklopi|zaženi|pošlji)(?!\\w)|(ali |a )?(mi )?(lahko|bi lahko) (mi )?(odpreš|predvajaš|zapreš|poiščeš|pokažeš|vklopiš|izklopiš|zaženeš|odprl|odprla|predvajal|predvajala)(?!\\w))|^((пусни|намери|потърси|спри|увеличи|намали|изключи|прати)(?!\\w)|(можеш ли|би ли могла|би ли могъл|може ли) (да )?(отвориш|пуснеш|затвориш|намериш|потърсиш|покажеш|спреш|включиш|изключиш)(?!\\w))|^((iftaħ|iftah|doqq|agħlaq|aghlaq|fittex|urini|waqqaf|ixgħel|ixghel|itfi|għolli|gholli|baxxi)(?!\\w)|(tista'?|tistax|tistgħu) (tiftaħ|tiftah|iddoqq|tagħlaq|taghlaq|tfittex|turini|twaqqaf|tixgħel|titfi)(?!\\w))|^((pecha(me|o|a|lo|la)?|am[oó]sa(me)?)(?!\\w)|(podes|poder[ií]as) (abrir|po[ñn]er|pechar|buscar|amosar|reproducir|tocar)(?!\\w))|^(ireki|itxi|bilatu|erakutsi|gelditu|bidali|erreproduzitu|piztu|itzali)(?!\\w)|(^|\\s)(ireki|itxi|bilatu|erakutsi|gelditu|bidali|erreproduzitu|piztu|itzali)( ezazu| ezazue| mesedez)?$|(ireki|itxi|bilatu|erakutsi|gelditu|bidali|piztu|itzali) (dezakezu|ahal duzu|al duzu)(?!\\w)|^((soek|wys my|stuur)(?!\\w)|maak .+ (oop|toe)$|sit (die|dit|hom|haar)( .+)? (aan|af|harder|sagter)$|(kan|sal) jy\\b.*\\b(oopmaak|toemaak|speel|soek|wys|stuur|aansit|afsit)(?!\\w))|^((opnaðu|opnadu|spilaðu|spiladu|lokaðu|lokadu|leitaðu|leitadu|sýndu|syndu|stöðvaðu|stodvadu|stoppaðu|stoppadu|sendu|hækkaðu|haekkadu|lækkaðu|laekkadu|kveiktu|slökktu|slokktu|settu)(?!\\w)|(geturðu|geturdu|gætirðu|gaetirdu)\\b.*\\b(opnað|opnad|spilað|spilad|lokað|lokad|leitað|leitad|sýnt|synt|sett|kveikt|slökkt)(?!\\w)|viltu (opna|loka|leita|sýna|syna|setja|kveikja|slökkva|slokkva|senda)(?!\\w))|^((hape|luaj|mbyll|mbylle|kërko|kerko|më trego|me trego|ndalo|dërgo|dergo|ndiz)(?!\\w)|(a )?(mund|mundesh|mundeni) (të |te |ta |t'i )?(hapësh|hapesh|luash|mbyllësh|mbyllesh|kërkosh|kerkosh|tregosh|ndezësh|ndezesh|dërgosh|dergosh)(?!\\w))|^((oscail|seinn|dún|cuardaigh|taispeáin|taispeain|seol|ardaigh|ísligh|isligh|múch)(?!\\w)|an (féidir|feidir) leat\\b.*\\b(oscailt|sheinm|sheinnt|chasadh|dhúnadh|dhunadh|chuardach|thaispeáint|thaispeaint|mhúchadh)(?!\\w))|^((agor|agorwch|chwarae|chwaraea|chwaraewch|caea|caewch|chwilia|chwiliwch|dangos|dangosa|dangoswch|stopia|anfon|anfona|anfonwch|diffodd|diffodda)(?!\\w)|(alli di|allet ti|wnei di|fedri di|gallwch chi|wnewch chi) (plîs |plis )?(agor|chwarae|cau|chwilio|dangos|anfon|diffodd|troi)(?!\\w))|^(адкрый|ўключы|уключы|прайграй|пастаў|зачыні|знайдзі|пакажы|спыні|выключы|дашлі|зрабі (гучней|цішэй)|(можаш|можаце|ці можаш|ці можаце) (ты |вы )?(адкрыць|уключыць|ўключыць|прайграць|паставіць|зачыніць|знайсці|паказаць|выключыць))(?!\\w)|^((пушти|најди|пребарај|запри|стопирај|испрати|засили|вклучи|исклучи)(?!\\w)|(можеш ли|дали можеш|може ли) (да )?(го |ја |ги )?(отвориш|пуштиш|затвориш|најдеш|пребараш|покажеш|вклучиш|исклучиш|испратиш)(?!\\w))|(^|\\s)(aç(?<!(karnım|karnımız|karnın)( (çok|aşırı|acayip|cidden|bayağı|baya|hala|hâlâ|de))? aç)(?<!^((ben|biz)( de)?( (çok|aşırı|acayip|cidden|bayağı|baya|hala|hâlâ))?|çok|aşırı|acayip|cidden|bayağı|baya|hala|hâlâ) aç)|bağla|oxut|qoş|axtar|göstər|dayandır|göndər|açarsan|aça bilərsən|bağlaya bilərsən|səsi artır|səsi azalt)( zəhmət olmasa)?$|ni (och|yop)( iltimos)?$|(^|\\s)(oching|ochib ber|yoping|qo['ʻ‘`]?y|qo['ʻ‘`]?ying|ijro et|ijro eting|qidir|qidiring|ko['ʻ‘`]?rsat|ko['ʻ‘`]?rsating|to['ʻ‘`]?xtat|to['ʻ‘`]?xtating|yubor|yuboring)( iltimos)?$|(^|\\s)(аш(?<!(қарным|қарнымыз|қарның|мен|біз|өте|қатты|әбден|тым) аш)|ашшы|ашыңыз|ашып бер|жап|жапшы|жабыңыз|қос|қосшы|қосыңыз|ойнат|ойнатшы|ізде|іздеші|іздеңіз|көрсет|көрсетші|көрсетіңіз|тоқтат|тоқтатшы|жібер|жіберші)( өтінемін)?$|(^|\\s)(нээ|нээгээч|нээж өг|нээнэ үү|хаагаач|тоглуул|тоглуулаач|тоглуулна уу|хайгаач|хайна уу|харуул|харуулаач|харуулна уу|зогсоо|зогсоогоорой|зогсооно уу|илгээ|илгээгээч|илгээнэ үү|асаа|асаагаач|асаана уу|унтраа|унтраагаач|унтраана уу)( л| дээ)?$|(^|\\s)(ख\u094bल|ख\u094bल\u093fद\u0947ऊ|ख\u094bल\u093fद\u093fन\u0941ह\u094bस\u094d|ख\u094bल\u094dन\u0941ह\u094bस\u094d|बज\u093eऊ|बज\u093eइद\u0947ऊ|बज\u093eउन\u0941ह\u094bस\u094d|चल\u093eऊ|चल\u093eइद\u0947ऊ|चल\u093eउन\u0941ह\u094bस\u094d|बन\u094dद गर|बन\u094dद गर\u093fद\u0947ऊ|बन\u094dद गर\u094dन\u0941ह\u094bस\u094d|ख\u094bज|ख\u094bज\u093fद\u0947ऊ|ख\u094bज\u094dन\u0941ह\u094bस\u094d|द\u0947ख\u093eऊ|द\u0947ख\u093eइद\u0947ऊ|द\u0947ख\u093eउन\u0941ह\u094bस\u094d|र\u094bक|र\u094bक\u094dन\u0941ह\u094bस\u094d|पठ\u093eऊ|पठ\u093eइद\u0947ऊ|पठ\u093eउन\u0941ह\u094bस\u094d)( न| ह\u0948| प\u094dल\u093fज)?[।.!?\\s]*$|^((tulung|mbok|coba) )*(bukak(en|na|ke)?|bukaen|puter(en|na|ke)|setel(en|na|ke)?|tutupen|tutupna|golek(na|ke|ana)|goleki|tuduh(na|ke)|mandheg(na|ke)|kirim(na|ke|en))(?!\\w)|^((don|dan) allah )?((ka|ki|ku) )?(buɗe|kunna|rufe|nuna min|tsayar|dakatar|kashe)(?!\\w)|^(don|dan) allah ((ka|ki|ku) )?bude(?!\\w)|\\b(za ka iya|za ki iya|kana iya|kina iya) (buɗe|bude|kunna|rufe|nemo|nuna|aika|kashe)(?!\\w)|^((jọ\u0300wọ\u0301|jọwọ|jowo) )?(ṣí|ṣi|ṣe orin|kọ orin|wá|fi hàn mí|fi han mi|dá dúró|fi ránṣẹ\u0301|fi ranse)(?!\\w)|^(biko )?(meghee|mepee|kpọọ|kpoo|mechie|chọọ|gosi m|kwụsị|kwusi|ziga|gbanye|gbanyụọ|gbanyuo)(?!\\w)|^((ngicela|ndicela|sicela) )?(vula|uvule|dlala|udlale|vala|uvale|sesha|usesha|khombisa|ukhombise|thumela|uthumele|khangela|ukhangele|bonisa|ubonise|yeka)(?!\\w)|\\bunga(vula|dlala|vala|sesha|khombisa|thumela|khangela|bonisa)(?!\\w)|^fadlan\\b.*\\b(fur|fura|shid|shida|xir|xidh|raadi|raadiya|jooji|dami|tus|dir)$|(-(ka|ga|ha|ta|da)|\\b(heesta|heesaha|muusigga|muusikada|fiidiyowga|barnaamijka|codka)) (fur|fura|shid|shida|xir|xidh|raadi|raadiya|jooji|dami)( fadlan)?$|\\bi tus( fadlan)?$|(^|\\s)(ክፈት|ክፈቺ|ክፈቱ|ክፈትልኝ|ክፈቺልኝ|ክፈቱልኝ|አጫውት|አጫውቺ|አጫውቱ|አጫውትልኝ|አጫውቺልኝ|አጫውቱልኝ|ዝጋ|ዝጊ|ዝጉ|ዝጋልኝ|ዝጊልኝ|ዝጉልኝ|ፈልግ|ፈልጊ|ፈልጉ|ፈልግልኝ|ፈልጊልኝ|ፈልጉልኝ|አሳየኝ|አሳይኝ|አሳዩኝ|አቁም|አቁሚ|አቁሙ|ላክ|ላኪ|ላኩ|ላክልኝ|ላኪልኝ|ላኩልኝ)( እባክህ| እባክሽ| እባካችሁ| እባክዎ)?[።፧!?.\\s]*$|(?<!(ပ\u102b\u1038စပ\u103a|ပ\u102b\u1038စပ\u103aက\u102d\u102f|န\u103e\u102fတ\u103a|င\u102d\u102fတ\u102c|င\u102d\u102fန\u1031တ\u102c|ရယ\u103aတ\u102c|အ\u1031\u102c\u103aတ\u102c) ?)(?<!(?<!အ|ပ\u103c\u102e\u1038|ဆ\u102d\u102f)ရင\u103a)(ဖ\u103dင\u1037\u103a(ပ\u103c)?|ပ\u102dတ\u103a|ရ\u103e\u102cဖ\u103d\u1031|ရ\u103e\u102c|ပ\u102d\u102f\u1037|ရပ\u103a|က\u103b\u1031\u102c\u103a)(လ\u102d\u102fက\u103a|ပ\u1031\u1038|စမ\u103a\u1038|ပ\u102b)+(ဦ\u1038|ဥ\\u102E\u1038)?(န\u1031\u102c\u103a)?[။၊!?.\\s]*$|အသ\u1036.*(ခ\u103b\u1032\u1037|လ\u103b\u103e\u1031\u102c\u1037|တ\u102d\u102f\u1038)(လ\u102d\u102fက\u103a|ပ\u1031\u1038|စမ\u103a\u1038|ပ\u102b)+(ဦ\u1038|ဥ\\u102E\u1038)?(န\u1031\u102c\u103a)?[။၊!?.\\s]*$|(ဖ\u103dင\u1037\u103a|ပ\u102dတ\u103a|ရ\u103e\u102c|ပ\u102d\u102f\u1037|ရပ\u103a|က\u103b\u1031\u102c\u103a)(လ\u102d\u102fက\u103a)?ပ\u1031\u1038(န\u102d\u102fင\u103aမလ\u102c\u1038|န\u102d\u102fင\u103aလ\u102c\u1038|မလ\u102c\u1038)|^(ဒ\u102b(က)? ?ဘ\u102c ?သ\u102eခ\u103bင\u103a\u1038|(ဒ\u102e ?|အခ\u102f ?(ဖ\u103dင\u1037\u103aန\u1031တ\u1032\u1037 ?)?)သ\u102eခ\u103bင\u103a\u1038(က)? ?(ဘ\u102c ?သ\u102eခ\u103bင\u103a\u1038|ဘ\u102c|န\u102cမည\u103a ?ဘ\u102c))(လ\u1032|လည\u103a\u1038)|(မ\u103bက\u103aန\u103e\u102cပ\u103cင\u103a|စခရင\u103a|screen)( ?က\u102d\u102f)? ?က\u103cည\u1037\u103a(လ\u102d\u102fက\u103a|ပ\u1031\u1038|စမ\u103a\u1038|ပ\u102b)+(ဦ\u1038|ဥ\\u102E\u1038)?(န\u1031\u102c\u103a)?[။၊!?.\\s]*$|^(မင\u103a\u1038|နင\u103a|သင\u103a|ရ\u103eင\u103a)? ?ဘ\u102c ?(တ\u103d\u1031)? ?မ\u103cင\u103a(ရ)?(လ\u1032|သလ\u1032)|(^|\\s)(გახსენი|გახსენით|ჩართე|ჩართეთ|დაუკარი|დაუკარით|დახურე|დახურეთ|გამორთე|გამორთეთ|მოძებნე|მოძებნეთ|მოიძიე|მაჩვენე|მაჩვენეთ|გააჩერე|გააჩერეთ|გაგზავნე|გაგზავნეთ|აუწიე|დაუწიე)(?!\\w)|ეს რა სიმღერაა|(ეკრანს|ეკრანზე) (შეხედე|დახედე|ნახე)|^რას ხედავ|(^|\\s)(բացիր|բացեք|միացրու|միացրեք|նվագիր|նվագեք|փակիր|փակեք|անջատիր|անջատեք|որոնիր|որոնեք|գտիր|գտեք|փնտրիր|ցույց տուր|ցույց տվեք|կանգնեցրու|դադարեցրու|ուղարկիր|ուղարկեք|բարձրացրու|իջեցրու)(?!\\w)|(^|\\s)(սա )?ինչ երգ է( սա| հիմա| նվագում| հնչում| միացված)*[.!?\\s]*$|նայիր (իմ )?էկրան|^ինչ ես տեսնում|^(בבקשה )?(פתח|פתחי|תפתח|תפתחי|נגן|נגני|תנגן|תנגני|סגור(?= )|סגרי|תסגור|תסגרי|חפש|חפשי|תחפש|תחפשי|תראה לי|תראי לי|הראה לי|הראי לי|עצור|עצרי|תעצור|תעצרי|הגבר|תגביר|תגבירי|הנמך|תנמיך|תנמיכי|שים(?! לב)|שימי(?! לב)|תשים|תשימי|הפעל|הפעילי|תפעיל|תפעילי|כבה|כבי|תכבה|תכבי|שלח|שלחי|תשלח|תשלחי)(?!\\w)|^(את יכולה|אתה יכול|תוכלי|תוכל|אפשר) (בבקשה )?ל(פתוח|נגן|סגור|חפש|הראות|הפעיל|כבות|שים|הגביר|הנמיך|שלוח)(?!\\w)|(کھولو|کھول دو|کھولیں|کھول دیں|چلاؤ|چلا دو|چلائیں|چلا دیں|بجاؤ|بجا دو|بجائیں|(?<!(بکواس|ڈرامہ|ڈرامے|مذاق|بولنا|رونا|چلانا|نخرے) )بند (کرو|کر دو|کریں|کر دیں)|ڈھونڈو|ڈھونڈ دو|تلاش کرو|تلاش کریں|دکھاؤ|دکھا دو|دکھائیں|روکو|روک دو|روکیں|بڑھاؤ|بڑھا دو|کم کرو|کم کر دو|بھیجو|بھیج دو)( نا| پلیز| پلز)?[۔.!?؟\\s]*$|(کھول|چلا|بجا|بند کر|دکھا|ڈھونڈ) (سکتے|سکتی|سکو) (ہو|ہیں|گے|گی)|کون سا گانا|(باز کن|بازش کن|باز کنید|پخش کن|پخشش کن|پخش کنید|بذار|بزار|بگذار|بذارش|ببند|ببندش|ببندید|جستجو کن|سرچ کن|بگرد|پیدا کن|نشون بده|نشونم بده|نشان بده|نشانم بده|متوقف کن|قطع کن|زیاد کن|زیادش کن|کم کن|کمش کن|بفرست|اجرا کن)( لطفا| لطفا\u064b| دیگه| پلیز)?[.!?؟\\s]*$|^(لطفا |لطفا\u064b )?(می[\\u200C ]?(تونی|توانی)|می[\\u200C ]?شه) .*(باز کنی|بازش کنی|پخش کنی|بذاری|بزاری|بگذاری|ببندی|جستجو کنی|بگردی|پیدا کنی|نشون بدی|نشان بدهی|اجرا کنی|بفرستی)|(^|\\s)(খ\u09cbল\u09cb|খ\u09c1ল\u09c7 দ\u09beও|খ\u09c1ল\u09c1ন|খ\u09cbল|চ\u09beল\u09beও|চ\u09beল\u09bfয\u09bc\u09c7 দ\u09beও|চ\u09beল\u09beন|চ\u09beল\u09c1 কর\u09cb|চ\u09beল\u09c1 কর\u09c7 দ\u09beও|ব\u09beজ\u09beও|ব\u09beজ\u09bfয\u09bc\u09c7 দ\u09beও|ব\u09beজ\u09beন|বন\u09cdধ কর\u09cb|বন\u09cdধ কর\u09c7 দ\u09beও|বন\u09cdধ কর\u09c1ন|বন\u09cdধ কর|খ\u09cb\u0981জ\u09cb|খ\u09c1\u0981জ\u09c7 দ\u09beও|খ\u09c1\u0981জ\u09c1ন|দ\u09c7খ\u09beও|দ\u09c7খ\u09bfয\u09bc\u09c7 দ\u09beও|দ\u09c7খ\u09beন|থ\u09beম\u09beও|থ\u09beম\u09bfয\u09bc\u09c7 দ\u09beও|থ\u09beম\u09beন|প\u09beঠ\u09beও|প\u09beঠ\u09bfয\u09bc\u09c7 দ\u09beও|প\u09beঠ\u09beন|ব\u09beড\u09bc\u09beও|ব\u09beড\u09bc\u09bfয\u09bc\u09c7 দ\u09beও|কম\u09beও|কম\u09bfয\u09bc\u09c7 দ\u09beও)( ন\u09be| প\u09cdল\u09bfজ| প\u09cdল\u09c0জ)?[।.!?\\s]*$|(খ\u09c1লত\u09c7|চ\u09beল\u09beত\u09c7|ব\u09beজ\u09beত\u09c7|বন\u09cdধ করত\u09c7|দ\u09c7খ\u09beত\u09c7|খ\u09c1\u0981জত\u09c7) (প\u09beরব\u09c7|প\u09beরব\u09c7ন|প\u09beর\u09cb|প\u09beর\u09c7ন|প\u09beরব\u09bf)|(^|\\s)(उघड|उघड\u093e|उघड\u0942न द\u0947|ल\u093eव|ल\u093eव\u093e|ल\u093eव\u0942न द\u0947|च\u093eल\u0942 कर|च\u093eल\u0942 कर\u093e|ब\u0902द कर|ब\u0902द कर\u093e|ब\u0902द कर\u0942न द\u0947|श\u094bध\u093e|श\u094bध\u0942न द\u0947|द\u093eखव|द\u093eखव\u093e|थ\u093e\u0902बव|थ\u093e\u0902बव\u093e|प\u093eठव|प\u093eठव\u093e|व\u093eढव|व\u093eढव\u093e|कम\u0940 कर|कम\u0940 कर\u093e)( न\u093e| प\u094dल\u0940ज)?[।.!?\\s]*$|(उघड\u0942|ल\u093eव\u0942|च\u093eल\u0942 कर\u0942|ब\u0902द कर\u0942|द\u093eखव\u0942|श\u094bध\u0942) (शकत\u0947स|शकत\u094bस|शकत\u093e|शक\u093eल) क\u093e|(^|\\s)(ખ\u0acbલ|ખ\u0acbલ\u0acb|ખ\u0acbલ\u0ac0 દ\u0ac7|ખ\u0acbલ\u0ac0 દ\u0acb|વગ\u0abeડ|વગ\u0abeડ\u0acb|વગ\u0abeડ\u0ac0 દ\u0ac7|વગ\u0abeડ\u0ac0 દ\u0acb|ચ\u0abeલ\u0ac1 કર|ચ\u0abeલ\u0ac1 કર\u0acb|બ\u0a82ધ કર|બ\u0a82ધ કર\u0acb|બ\u0a82ધ કર\u0ac0 દ\u0ac7|બ\u0a82ધ કર\u0ac0 દ\u0acb|શ\u0acbધ|શ\u0acbધ\u0acb|શ\u0acbધ\u0ac0 દ\u0ac7|બત\u0abeવ|બત\u0abeવ\u0acb|ર\u0acbક\u0acb|મ\u0acbકલ|મ\u0acbકલ\u0acb|વધ\u0abeર\u0acb|ઘટ\u0abeડ\u0acb)( ન\u0ac7| પ\u0acdલ\u0ac0ઝ)?[.!?\\s]*$|(ખ\u0acbલ\u0ac0|વગ\u0abeડ\u0ac0|ચ\u0abeલ\u0ac1 કર\u0ac0|બ\u0a82ધ કર\u0ac0|બત\u0abeવ\u0ac0|શ\u0acbધ\u0ac0) (શક\u0ac7|શક\u0ac0શ|શક\u0acb|શકશ\u0acb)|(^|\\s)(ਖ\u0a4bਲ\u0a4dਹ\u0a4b|ਖ\u0a4bਲ\u0a4b|ਖ\u0a4bਲ\u0a4dਹ ਦ\u0a3fਓ|ਖ\u0a4bਲ\u0a4dਹ ਦ\u0a47|ਖ\u0a4bਲ ਦ\u0a3fਓ|ਖ\u0a4bਲ ਦ\u0a47|ਚਲ\u0a3eਓ|ਚਲ\u0a3e ਦ\u0a3fਓ|ਚਲ\u0a3e ਦ\u0a47|ਵਜ\u0a3eਓ|ਵਜ\u0a3e ਦ\u0a3fਓ|ਵਜ\u0a3e ਦ\u0a47|ਬ\u0a70ਦ ਕਰ\u0a4b|ਬ\u0a70ਦ ਕਰ ਦ\u0a3fਓ|ਬ\u0a70ਦ ਕਰ ਦ\u0a47|ਲ\u0a71ਭ\u0a4b|ਲ\u0a71ਭ ਦ\u0a3fਓ|ਦ\u0a3fਖ\u0a3eਓ|ਦ\u0a3fਖ\u0a3e ਦ\u0a3fਓ|ਰ\u0a4bਕ\u0a4b|ਰ\u0a4bਕ ਦ\u0a3fਓ|ਭ\u0a47ਜ\u0a4b|ਭ\u0a47ਜ ਦ\u0a3fਓ|ਵਧ\u0a3eਓ|ਘਟ\u0a3eਓ)( ਨ\u0a3e| ਪਲ\u0a40ਜ\u0a3c| ਪਲ\u0a40ਜ)?[।.!?\\s]*$|(ਖ\u0a4bਲ\u0a4dਹ|ਖ\u0a4bਲ|ਚਲ\u0a3e|ਵਜ\u0a3e|ਬ\u0a70ਦ ਕਰ|ਦ\u0a3fਖ\u0a3e|ਲ\u0a71ਭ) (ਸਕਦ\u0a47|ਸਕਦ\u0a40|ਸਕ\u0a4bਗ\u0a47|ਸਕ\u0a4bਗ\u0a40)|(^|\\s)(த\u0bbfற|த\u0bbfறங\u0bcdக|த\u0bbfறக\u0bcdகவ\u0bc1ம\u0bcd|த\u0bbfறந\u0bcdத\u0bc1 வ\u0bbfட\u0bc1|இயக\u0bcdக\u0bc1|இயக\u0bcdக\u0bc1ங\u0bcdக|இயக\u0bcdகவ\u0bc1ம\u0bcd|ப\u0bcbட\u0bc1|ப\u0bcbட\u0bc1ங\u0bcdக|ப\u0bcbட\u0bcdட\u0bc1 வ\u0bbfட\u0bc1|ம\u0bc2ட\u0bc1|ம\u0bc2ட\u0bc1ங\u0bcdக|ம\u0bc2டவ\u0bc1ம\u0bcd|ம\u0bc2ட\u0bbf வ\u0bbfட\u0bc1|த\u0bc7ட\u0bc1|த\u0bc7ட\u0bc1ங\u0bcdக|த\u0bc7டவ\u0bc1ம\u0bcd|க\u0bbeட\u0bcdட\u0bc1|க\u0bbeட\u0bcdட\u0bc1ங\u0bcdக|க\u0bbeட\u0bcdடவ\u0bc1ம\u0bcd|ந\u0bbfற\u0bc1த\u0bcdத\u0bc1|ந\u0bbfற\u0bc1த\u0bcdத\u0bc1ங\u0bcdக|ந\u0bbfற\u0bc1த\u0bcdதவ\u0bc1ம\u0bcd|அன\u0bc1ப\u0bcdப\u0bc1|அன\u0bc1ப\u0bcdப\u0bc1ங\u0bcdக|அன\u0bc1ப\u0bcdபவ\u0bc1ம\u0bcd|(ஓபன\u0bcd|ஓப\u0bcdபன\u0bcd|ப\u0bcdள\u0bc7|ப\u0bbfள\u0bc7|க\u0bcdள\u0bcbஸ\u0bcd|சர\u0bcdச\u0bcd|ஸ\u0bcdட\u0bbeப\u0bcd) (பண\u0bcdண\u0bc1|பண\u0bcdண\u0bc1ங\u0bcdக|ச\u0bc6ய\u0bcd|ச\u0bc6ய\u0bcdயவ\u0bc1ம\u0bcd))( ப\u0bcdள\u0bc0ஸ\u0bcd| ப\u0bbfள\u0bc0ஸ\u0bcd)?[.!?\\s]*$|(த\u0bbfறக\u0bcdக|இயக\u0bcdக|ப\u0bcbட|ம\u0bc2ட|த\u0bc7ட|க\u0bbeட\u0bcdட) ம\u0bc1ட\u0bbfய\u0bc1ம\u0bbe|(^|\\s)(త\u0c46ర\u0c41వ\u0c41|త\u0c46రవ\u0c02డ\u0c3f|త\u0c46ర\u0c3fచ\u0c3f ప\u0c46ట\u0c4dట\u0c41|ప\u0c4dల\u0c47 చ\u0c47య\u0c3f|ప\u0c4dల\u0c47 చ\u0c46య\u0c4dయ\u0c3f|ప\u0c4dల\u0c47 చ\u0c47య\u0c02డ\u0c3f|ప\u0c46ట\u0c4dట\u0c41|ప\u0c46ట\u0c4dట\u0c02డ\u0c3f|మ\u0c42స\u0c3fవ\u0c47య\u0c3f|మ\u0c42స\u0c47య\u0c3f|మ\u0c42స\u0c3fవ\u0c47య\u0c02డ\u0c3f|వ\u0c46త\u0c41క\u0c41|వ\u0c46తక\u0c02డ\u0c3f|చ\u0c42ప\u0c3f\u0c02చ\u0c41|చ\u0c42ప\u0c3f\u0c02చ\u0c02డ\u0c3f|ఆప\u0c41|ఆప\u0c02డ\u0c3f|ప\u0c02ప\u0c41|ప\u0c02ప\u0c02డ\u0c3f|ప\u0c46\u0c02చ\u0c41|ప\u0c46\u0c02చ\u0c02డ\u0c3f|తగ\u0c4dగ\u0c3f\u0c02చ\u0c41|తగ\u0c4dగ\u0c3f\u0c02చ\u0c02డ\u0c3f|(ఓప\u0c46న\u0c4d|క\u0c4dల\u0c4bజ\u0c4d|స\u0c46ర\u0c4dచ\u0c4d|స\u0c4dట\u0c3eర\u0c4dట\u0c4d|స\u0c4dట\u0c3eప\u0c4d) (చ\u0c47య\u0c3f|చ\u0c46య\u0c4dయ\u0c3f|చ\u0c47య\u0c02డ\u0c3f))( ప\u0c4dల\u0c40జ\u0c4d)?[.!?\\s]*$|(త\u0c46రవ|ప\u0c4dల\u0c47 చ\u0c47య|ప\u0c46ట\u0c4dట|మ\u0c42స\u0c3fవ\u0c47య|వ\u0c46తక|చ\u0c42ప\u0c3f\u0c02చ)గల(వ\u0c3e|ర\u0c3e)|(^|\\s)(ತ\u0cc6ರ\u0cc6|ತ\u0cc6ರ\u0cc6ಯ\u0cbfರ\u0cbf|ತ\u0cc6ರ\u0cc6ದ\u0cc1 ಬ\u0cbfಡ\u0cc1|ಪ\u0ccdಲ\u0cc7 ಮ\u0cbeಡ\u0cc1|ಪ\u0ccdಲ\u0cc7 ಮ\u0cbeಡ\u0cbf|ಹ\u0cbeಕ\u0cc1|ಹ\u0cbeಕ\u0cbf|ಮ\u0cc1ಚ\u0ccdಚ\u0cc1|ಮ\u0cc1ಚ\u0ccdಚ\u0cbf|ಹ\u0cc1ಡ\u0cc1ಕ\u0cc1|ಹ\u0cc1ಡ\u0cc1ಕ\u0cbf|ತ\u0ccbರ\u0cbfಸ\u0cc1|ತ\u0ccbರ\u0cbfಸ\u0cbf|ನ\u0cbfಲ\u0ccdಲ\u0cbfಸ\u0cc1|ನ\u0cbfಲ\u0ccdಲ\u0cbfಸ\u0cbf|ಕಳ\u0cc1ಹ\u0cbfಸ\u0cc1|ಕಳ\u0cc1ಹ\u0cbfಸ\u0cbf|ಹ\u0cc6ಚ\u0ccdಚ\u0cbfಸ\u0cc1|ಹ\u0cc6ಚ\u0ccdಚ\u0cbfಸ\u0cbf|ಕಡ\u0cbfಮ\u0cc6 ಮ\u0cbeಡ\u0cc1|ಕಡ\u0cbfಮ\u0cc6 ಮ\u0cbeಡ\u0cbf|(ಓಪನ\u0ccd|ಕ\u0ccdಲ\u0ccbಸ\u0ccd|ಸರ\u0ccdಚ\u0ccd|ಸ\u0ccdಟ\u0cbeರ\u0ccdಟ\u0ccd|ಸ\u0ccdಟ\u0cbeಪ\u0ccd) (ಮ\u0cbeಡ\u0cc1|ಮ\u0cbeಡ\u0cbf))( ಪ\u0ccdಲ\u0cc0ಸ\u0ccd)?[.!?\\s]*$|(ತ\u0cc6ರ\u0cc6ಯ|ಪ\u0ccdಲ\u0cc7 ಮ\u0cbeಡ|ಹ\u0cbeಕ|ಮ\u0cc1ಚ\u0ccdಚ|ಹ\u0cc1ಡ\u0cc1ಕ|ತ\u0ccbರ\u0cbfಸ)ಬಹ\u0cc1ದ\u0cbe|(^|\\s)(ത\u0d41റക\u0d4dക\u0d42|ത\u0d41റക\u0d4dക\u0d4d|ത\u0d41റന\u0d4dന\u0d41 തര\u0d42|പ\u0d4dല\u0d47 ച\u0d46യ\u0d4dയ\u0d42|പ\u0d4dല\u0d47 ച\u0d46യ\u0d4dയ\u0d4d|ഇട\u0d42|ഇട\u0d4d|അടയ\u0d4dക\u0d4dക\u0d42|അടയ\u0d4dക\u0d4dക\u0d4d|അടച\u0d4dച\u0d41 തര\u0d42|ത\u0d3fരയ\u0d42|ത\u0d3fരയ\u0d4d|തപ\u0d4dപ\u0d4d|ക\u0d3eണ\u0d3fക\u0d4dക\u0d42|ക\u0d3eണ\u0d3fക\u0d4dക\u0d4d|ന\u0d3fർത\u0d4dത\u0d42|ന\u0d3fർത\u0d4dത\u0d4d|അയയ\u0d4dക\u0d4dക\u0d42|അയയ\u0d4dക\u0d4dക\u0d4d|ക\u0d42ട\u0d4dട\u0d42|ക\u0d41റയ\u0d4dക\u0d4dക\u0d42|ക\u0d41റയ\u0d4dക\u0d4dക\u0d4d|(ഓപ\u0d4dപൺ|ക\u0d4dല\u0d4bസ\u0d4d|സ\u0d46ർച\u0d4dച\u0d4d|സ\u0d4dറ\u0d4dറ\u0d3eർട\u0d4dട\u0d4d|സ\u0d4dറ\u0d4dറ\u0d4bപ\u0d4dപ\u0d4d) (ച\u0d46യ\u0d4dയ\u0d42|ച\u0d46യ\u0d4dയ\u0d4d))( പ\u0d4dല\u0d40സ\u0d4d)?[.!?\\s]*$|(ത\u0d41റക\u0d4dക|പ\u0d4dല\u0d47 ച\u0d46യ\u0d4dയ|ഇട|അടയ\u0d4dക\u0d4dക|ത\u0d3fരയ|ക\u0d3eണ\u0d3fക\u0d4dക)\u0d3eമ\u0d4b|(ത\u0d41റക\u0d4dക\u0d3eൻ|പ\u0d4dല\u0d47 ച\u0d46യ\u0d4dയ\u0d3eൻ|ഇട\u0d3eൻ|അടയ\u0d4dക\u0d4dക\u0d3eൻ) പറ\u0d4dറ\u0d41മ\u0d4b|\\b(open|play|close|search|start|stop|pause|skip) (pannu|pannunga|panni vidu|cheyyi|chey|cheyandi|cheyyandi|maadu|maadi|madu|madi|cheyyu|cheyy)( da| di| please| pls| plz)?[.!?\\s]*$", RegexOptions.IgnoreCase);

	private static readonly Regex Errands = new Regex("^(send|dm|pm|reply to|respond to)\\b(?!\\s+(me|us|help)\\b)|^(message|msg|text|ping) (?!(is|was|are|were|from|looks|said|says|me|us)\\b)\\w|^(post|tweet) (this|that|it|a|an|my|our|something|on|in|to)\\b|^(tell|ask|say|let|invite|kick|ban|add|react|call|ring|share|pin|block|unblock|unfriend) .+\\b(on|in|into|through|via|over|to) (the |my )?(#\\S+|discord|slack|teams|whatsapp|telegram|signal|messenger|skype|instagram|insta|snapchat|snap|reddit|twitter|tiktok|facebook|roblox|steam|(group )?chat|call|vc|server|channel|gc|dms?)(?!\\w)|^type (?!(of|is|was)\\b)\\w|^(click|tap|double click|right click) (?!(bait|is|was)\\b)\\w|^scroll (up|down|left|right|to|through)\\b|^(fill (in|out)|sign in|log in)\\b|^(end|leave|hang up|disconnect( from)?|join|accept|decline|pick up|answer|reject|hop (in|on|off)|get off) (the |my |this |that |a |our |their )?(\\w+('s)? )?(call|vc|voice (chat|channel|call)|video call|meeting|stream|server|channel)\\b|^(hang up|disconnect|hop off|get off)( (the )?(call|vc))?$", RegexOptions.IgnoreCase);

	private static readonly Regex Opener = new Regex("^((great|nice|cool|sweet|perfect|awesome|amazing|alright|alr|aight|bet|thanks|thank you|thx|ty|so|and|now|also|oh|wait|um|uh|hmm|lol|lmao|bro|dude|bruh)[\\s,!.]+)+(?=((hey|yo|ok|okay)[\\s,]+)?(capri\\b|(can|could|would|will) (you|u)\\b|please\\b|pls\\b|plz\\b))", RegexOptions.IgnoreCase);

	internal static Func<List<InstalledApp>> Catalog = ReadCatalog;

	internal static Func<string, bool> Exists = File.Exists;

	internal static Action<string> Run = Start;

	internal static Action<byte> Press = Tap;

	internal static Func<string, string> Look = (string param0) => "You can't see their screen right now.";

	internal static Func<string, string> Fetch = Download;

	internal static Func<List<OpenWindow>> Windows = ReadWindows;

	internal static Func<OpenWindow, string, bool> Steer = Apply;

	internal static Func<string> Unlisted = () => (string)null;

	internal static bool Presses = true;

	internal static Func<string, string> Instead = (string param0) => (string)null;

	internal static Func<string, bool> PressPlay = ClickPlay;

	internal static Func<string> NowPlaying = SpotifyTitle;

	internal static Func<string, string, string> Work = (string param0, string param1) => "Using the mouse and keyboard isn't ready on this PC yet.";

	internal static Func<string, string> Resume = (string param0) => (string)null;

	internal static Func<string, string> Hush = (string text) => text;

	internal static Action<byte[]> Chord = PressChord;

	internal static Func<string, int, bool> Power = Switch;

	internal static Func<bool, string> Nap = (bool param0) => "Couldn't nap just now.";

	internal static Func<string, string, string> MoveSelf = (string param0, string param1) => "Couldn't move just now.";

	internal static Func<string, string> Animate = (string param0) => "Couldn't do that just now.";

	private static readonly string[] Browsers = new string[6] { "chrome", "msedge", "firefox", "brave", "opera", "vivaldi" };

	private static readonly string[] Shell = new string[4] { "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd" };

	private static readonly Regex Junk = new Regex("\\b(uninstall\\w*|desinstall\\w*|readme|read me|lisez moi|help|aide|manual|manuel|documentation|release notes|notes de version|licen[cs]e|website|site web|support|assistance|changelog)\\b", RegexOptions.IgnoreCase);

	private static readonly string[,] Searches = new string[8, 2]
	{
		{ "youtube", "https://www.youtube.com/results?search_query=" },
		{ "google", "https://www.google.com/search?q=" },
		{ "twitch", "https://www.twitch.tv/search?term=" },
		{ "reddit", "https://www.reddit.com/search/?q=" },
		{ "wikipedia", "https://en.wikipedia.org/w/index.php?search=" },
		{ "spotify", "https://open.spotify.com/search/" },
		{ "roblox", "https://www.roblox.com/discover/?Keyword=" },
		{ "github", "https://github.com/search?q=" }
	};

	private static readonly object gate = new object();

	private static List<InstalledApp> cached;

	private static DateTime cachedAt;

	private static readonly string[] Swaps = new string[22]
	{
		"\\bi'?m\\b", "they're", "\\bi am\\b", "they are", "\\bi was\\b", "they were", "\\bi've\\b", "they've", "\\bi'll\\b", "they'll",
		"\\bi'd\\b", "they'd", "\\bi\\b", "they", "\\bmyself\\b", "themselves", "\\bmy\\b", "their", "\\bmine\\b", "theirs",
		"\\bme\\b", "them"
	};

	internal static Func<string, string> Rename;

	private static readonly Regex Polite = new Regex("\\b(please|pls|plz|pleas|thank you|thanks|por favor|porfavor|porfa|porfis|gracias|merci|s'il (te|vous) pla[iî]t|sil (te|vous) pla[iî]t|stp|svp|bitte|danke|per favore|perfavore|per piacere|grazie|pfv|obrigad[oa]|si us plau|sisplau|siusplau|gr[àa]cies|grazas|mesedez|eskerrik asko|alsjeblieft|alstublieft|a\\.?u\\.?b|dank je|dankjewel|bedankt|asseblief|dankie|snälla|snalla|tack|vær så snill|vennligst|venligst|takk|tak|vinsamlegast|ole hyvä|ole kiltti|kiitos|palun|aitäh|aitah|lūdzu|ludzu|paldies|prašau|prasau|ačiū|aciu|proszę|prosze|dzięki|dzieki|dziękuję|dziekuje|prosím|prosim|díky|diky|děkuji|dekuji|ďakujem|dakujem|kérlek|kerlek|légyszi|legyszi|köszi|koszi|köszönöm|hvala|molim|молим|хвала|të lutem|te lutem|ju lutem|faleminderit|te rog|vă rog|va rog|mersi|mulțumesc|multumesc|le do thoil|go raibh maith agat|plîs|plis|os gwelwch yn dda|diolch|jekk jogħġbok|jekk joghgbok|grazzi|lütfen|lutfen|teşekkürler|tesekkurler|sağ ?ol|sag ?ol|zəhmət olmasa|xahiş edirəm|təşəkkürlər|iltimos|rahmat|пожалуйста|пожалуста|плиз|плз|пж|спасибо|будь ласка|дякую|калі ласка|дзякуй|моля|благодаря|мерси|те молам|ве молам|фала|благодарам|өтінемін|рахмет|гуйя|баярлалаа|παρακαλώ|παρακαλω|ευχαριστώ|ευχαριστω|làm ơn|lam on|cảm ơn|cám ơn|cam on|tolong|mohon|terima kasih|makasih|sila|tulung|mangga|matur nuwun|suwun|pakiusap|paki\\w*|salamat|tafadhali|asante|don allah|dan allah|na gode|jọ\u0300wọ\u0301|jọwọ|jowo|o ṣeun|o seun|biko|daalụ|daalu|ngiyacela|ngicela|sicela|ngiyabonga|siyabonga|ndiyacela|ndicela|enkosi|fadlan|mahadsanid|من فضلك|لو سمحت|رجاء|رجاء\u064b|الرجاء|شكرا|شكرا\u064b|براہ کرم|مہربانی|پلیز|شکریہ|لطفا|لطفا\u064b|ممنون|مرسی|בבקשה|תודה|እባክህ|እባክሽ|እባካችሁ|እባክዎ|አመሰግናለሁ|እናመሰግናለን|გთხოვ|გთხოვთ|მადლობა|გმადლობ|გმადლობთ|խնդրում եմ|խնդրեմ|շնորհակալություն|շնորհակալ եմ|մերսի)\\b|请|請|麻烦|麻煩|拜托|拜託|谢谢|謝謝|ください|下さい|お願い|おねがい|ありがとう|주세요|부탁|제발|고마워|감사|กร\u0e38ณา|รบกวน|ขอบค\u0e38ณ|ກະລ\u0eb8ນາ|ຂອບໃຈ|ស\u17bcម|អរគ\u17bbណ|က\u103b\u1031\u1038ဇ\u1030\u1038ပ\u103c\u102f၍|က\u103b\u1031\u1038ဇ\u1030\u1038ပ\u103c\u102fပ\u103c\u102e\u1038|က\u103b\u1031\u1038ဇ\u1030\u1038တင\u103a|က\u103b\u1031\u1038ဇ\u1030\u1038ပ\u1032|ပ\u102b(ဦ\u1038|ဥ\\u102E\u1038|န\u1031\u102c\u103a)?[။၊!?.\\s]*$|क\u0943पय\u093e|प\u094dल\u0940ज\u093c|प\u094dल\u0940ज|प\u094dल\u093fज|धन\u094dयव\u093eद|श\u0941क\u094dर\u093fय\u093e|দয\u09bc\u09be কর\u09c7|প\u09cdল\u09bfজ|প\u09cdল\u09c0জ|ধন\u09cdযব\u09beদ|ક\u0ac3પ\u0abe કર\u0ac0ન\u0ac7|પ\u0acdલ\u0ac0ઝ|આભ\u0abeર|ਕ\u0a3fਰਪ\u0a3e ਕਰਕ\u0a47|ਪਲ\u0a40ਜ\u0a3c|ਪਲ\u0a40ਜ|ਧ\u0a70ਨਵ\u0a3eਦ|தயவ\u0bc1ச\u0bc6ய\u0bcdத\u0bc1|ப\u0bcdள\u0bc0ஸ\u0bcd|ப\u0bbfள\u0bc0ஸ\u0bcd|நன\u0bcdற\u0bbf|దయచ\u0c47స\u0c3f|ప\u0c4dల\u0c40జ\u0c4d|ధన\u0c4dయవ\u0c3eద\u0c3eల\u0c41|ದಯವ\u0cbfಟ\u0ccdಟ\u0cc1|ಪ\u0ccdಲ\u0cc0ಸ\u0ccd|ಧನ\u0ccdಯವ\u0cbeದ|ദയവ\u0d3eയ\u0d3f|പ\u0d4dല\u0d40സ\u0d4d|നന\u0d4dദ\u0d3f", RegexOptions.IgnoreCase);

	private static readonly Regex JustPlease = new Regex("^\\W*((ok|okay|fine|sorry|vale|perd[oó]n|d[ée]sol[ée])\\W+)?(pretty\\s+)?(please+|pls|plz|pleas+e*|por favor|porfavor|porfa|porfis|s'il (te|vous) pla[iî]t|sil (te|vous) pla[iî]t|stp|svp|bitte( sehr| schön)?|per favore|perfavore|per piacere|pfv|si us plau|sisplau|siusplau|mesedez|alsjeblieft|alstublieft|a\\.?u\\.?b\\.?|asseblief|snälla|snalla|vær så snill|vennligst|venligst|vinsamlegast|ole hyvä|ole kiltti|palun|lūdzu|ludzu|prašau|prasau|proszę|prosze|prosím|prosim|kérlek|kerlek|légyszi|legyszi|molim( te| vas)?|молим( те)?|të lutem|te lutem|ju lutem|te rog|vă rog|va rog|le do thoil|plîs|plis|os gwelwch yn dda|jekk jogħġbok|jekk joghgbok|lütfen|lutfen|zəhmət olmasa|xahiş edirəm|iltimos|пожалуйста|пожалуста|плиз|плз|пж|будь ласка|калі ласка|моля|те молам|ве молам|өтінемін|гуйя|παρακαλώ|παρακαλω|làm ơn|lam on|tolong|mohon|sila|tulung|mangga|pakiusap|tafadhali|don allah|dan allah|jọ\u0300wọ\u0301|jọwọ|jowo|biko|ngiyacela|ngicela|sicela|ndiyacela|ndicela|fadlan|请|請|拜托|拜託|お願い(します)?|おねがい(します)?|ください|부탁(해|해요|합니다)|제발|กร\u0e38ณา|ขอร\u0e49อง|ກະລ\u0eb8ນາ|ស\u17bcម(ម\u17c1ត\u17d2ត\u17b6)?|က\u103b\u1031\u1038ဇ\u1030\u1038ပ\u103c\u102f၍|က\u103b\u1031\u1038ဇ\u1030\u1038ပ\u103c\u102fပ\u103c\u102e\u1038|क\u0943पय\u093e|प\u094dल\u0940ज\u093c|प\u094dल\u0940ज|प\u094dल\u093fज|দয\u09bc\u09be কর\u09c7|প\u09cdল\u09bfজ|প\u09cdল\u09c0জ|ક\u0ac3પ\u0abe કર\u0ac0ન\u0ac7|પ\u0acdલ\u0ac0ઝ|ਕ\u0a3fਰਪ\u0a3e ਕਰਕ\u0a47|ਪਲ\u0a40ਜ\u0a3c|ਪਲ\u0a40ਜ|தயவ\u0bc1ச\u0bc6ய\u0bcdத\u0bc1|ப\u0bcdள\u0bc0ஸ\u0bcd|ப\u0bbfள\u0bc0ஸ\u0bcd|దయచ\u0c47స\u0c3f|ప\u0c4dల\u0c40జ\u0c4d|ದಯವ\u0cbfಟ\u0ccdಟ\u0cc1|ಪ\u0ccdಲ\u0cc0ಸ\u0ccd|ദയവ\u0d3eയ\u0d3f|പ\u0d4dല\u0d40സ\u0d4d|من فضلك|لو سمحت|رجاء|رجاء\u064b|الرجاء|براہ کرم|پلیز|لطفا|لطفا\u064b|בבקשה|እባክህ|እባክሽ|እባካችሁ|እባክዎ|გთხოვ|გთხოვთ|խնդրում եմ|խնդրեմ)(\\s+capri)?\\W*$", RegexOptions.IgnoreCase);

	private static readonly Random Dice = new Random();

	internal static Func<int> PleaseRoll = () =>
	{
		lock (Dice)
		{
			return Dice.Next(10);
		}
	};

	private static string heldForPlease;

	private static readonly Regex Unknown = new Regex("\\b(can'?t|cannot|can not|unable to|don'?t have|no way to|not able to)\\b[^.!?]{0,40}\\b(check|browse|look|search|google|access|know|tell|real[- ]?time|live|internet|web|online|current|up[- ]to[- ]date|forecast)\\b|\\b(i'?ll|i will|let me|lemme|i'?m (gonna|going to)|gonna|i gotta|i have to|i need to)\\s+(\\w+\\s+){0,2}(search|google|look (it |that |this )?up|check)\\b|\\b(you('ll)? (might|may|will|would)? ?(need|want|have) to|maybe (you can|try)|you (gotta|got to|could|can|should)|try)\\s+(\\w+\\s+){0,3}(look|check|search|google)\\b|\\bcheck (a|the|your) (\\w+\\s+){0,2}(weather|news|app|site|feed|forecast|website)\\b|\\bcrystal ball\\b", RegexOptions.IgnoreCase);

	private static readonly Regex Question = new Regex("\\?\\s*$|^(what|what's|whats|how|how's|when|where|who|which|why|is|are|was|were|will|does|do|did|can|could|would|tell me|check|look up|google|search|any idea)\\b", RegexOptions.IgnoreCase);

	private static readonly Regex Promise = new Regex("\\b(i'?ll|i will|let me|lemme|i'?m (gonna|going to|about to)|gonna|on it,?)[\\s—–-]+(\\w+[\\s,—–-]+){0,2}(open|launch|load|boot|fire|pull|bring|search|google|look up|click|type|close|switch|head|install|download|play(?!\\s+(along|around|with|nice|fair|dumb|it (cool|safe)|((a|some|the|this|that|any|your|my)\\s+)?(games?|round|match)\\b|devil))|put on|queue|blast|spin|mute|unmute|deafen|undeafen|send(?!\\s+(you|u|ya)\\b)|message(?!\\s+(you|u)\\b)|text(?!\\s+(you|u)\\b)|dm|ping|reply|post|hang up|(end|leave|join) (the|your|this|that) (call|vc|meeting))\\b|^((sure|ok|okay|alright|bet|got it|on it|say less|yessir+|yezzir+)[,.!\\s—–-]+)?((choom|chooms)[,.!\\s—–-]+)?(opening|launching|loading|pulling up|bringing up|firing up|booting up|heading to|searching|clicking|typing|closing|switching|playing|putting on|queuing|queueing|blasting|spinning|muting|unmuting|deafening|sending|messaging|texting|dming|replying|posting|hanging up|ending (the|your) call|leaving (the|your) (call|vc)|joining (the|your) (call|vc)|disconnecting)\\b|\\bi'?m\\s+(now\\s+)?(sending|messaging|texting|dming|opening(?!\\s+up\\b)|launching|closing|muting|unmuting|deafening|hanging up|posting|replying)\\b|^\\W*((ok|okay|sure|bet|alright)[,.!\\s—–-]+)?(i'?m\\s+)?on it\\b|\\b(call|vc|message|dm|text)\\s+(ended|sent|posted)\\b|^\\W*((ok|okej|okay|jasne|dobra|spoko|już|juz|sure|vale|claro|vamos|d'accord|ok[eé]|alles klar|klar|certo|va bene|хорошо|ладно|окей|сейчас)[,.!\\s—–-]+)*((choom|chooms)[,.!\\s—–-]+)?(otwieram|odpalam|włączam|wlaczam|uruchamiam|puszczam|zamykam|wysyłam|wysylam|otworzę|otworze|włączę|wlacze|odpalę|odpale|puszczę|puszcze|uruchomię|uruchomie|otworzyłam|otworzylam|włączyłam|wlaczylam|odpaliłam|odpalilam|puściłam|puscilam|uruchomiłam|uruchomilam|abriendo|abro|pongo|reproduciendo|abr[ií]|puse|te abro|te pongo|j'ouvre|je lance|je mets|j'ai ouvert|j'ai lanc[ée]|ich [öo]ffne|ich starte|ich spiele|ich habe (\\w+\\s+){0,3}(ge[öo]ffnet|gestartet)|abrindo|vou abrir|abri|coloquei|apro|metto|ho aperto|открываю|включаю|запускаю|открыла|включила|запустила)(?!\\w)|^((done|sorted|there|boom|bet|ok|okay|alright)[,.!\\s—–-]+)?(i('ve| have)\\s+)?(just\\s+)?(opened|launched|closed|sent(?!\\s+(you|u|ya)\\b)|messaged|texted|dmed|dm'd|posted|replied|muted|unmuted|deafened|undeafened|hung up|(ended|left|joined) (the|your|this|that) (call|vc|meeting|server|channel))\\b", RegexOptions.IgnoreCase);

	private static readonly Regex Warmth = new Regex("\\b(send(s|ing)?|sent)\\s+((you|u|ya|y'all)\\s+)?((a|an|the|my|some|all|so|much|many|lots|of|this|big|bigger|biggest|huge|massive|giant|tight|warm|warmest|virtual|good|positive|happy|healing|cozy|cosy|little|tiny|extra|more|mega|super|best|sweet|soft|gentle|digital|cyber|neon)\\s+)*(hugs?|huggies|squeezes?|kiss(es)?|love|xoxo|vibes?|energy|thoughts|wishes|prayers?|luck|cuddles?|smooches?|hearts?|sunshine|strength|support|positivity|juju|blessings?|regards|cheers|kudos|props|high[- ]?fives?|fist[- ]?bumps?|headpats?|pats)\\b(?!\\s+(to|over to)\\s+(?!(you|u|ya|y'all|yourself)\\b)\\w)", RegexOptions.IgnoreCase);

	private static readonly Regex Credit = new Regex("\\b(you|ya|u)('ve| have)?\\s+(just\\s+|already\\s+|finally\\s+|actually\\s+)?(opened|typed|launched|sent|closed|played|paused|skipped|muted|unmuted|deafened|undeafened|clicked|searched|posted|messaged|texted|dmed|dm'd|replied|wrote|pulled up|fired up|booted up|hung up|ended|left|joined)\\b", RegexOptions.IgnoreCase);

	private static readonly Regex Doing = new Regex("^((i (want|need) (you|u) to|go ahead and|just|now|then|and)\\s+)*(send|dm|pm|message|msg|text|ping|tell|say|ask|type|write|post|tweet|reply|respond|put|paste|share|open|launch|start|run|close|quit|exit|kill|join|leave|end|hang up|call|ring|turn|shut|power|switch|restart|reboot|sleep|lock|log|sign|mute|unmute|deafen|undeafen|play|pause|skip|go|click|tap|press|scroll|search|google|find|make|create|delete|remove|add|invite|kick|ban|block|unblock|react|pin|accept|decline|install|uninstall|download|change|set|minimi[sz]e|maximi[sz]e|move|copy|rename|save|record|clip|fill|submit|hop|get on|get off|pick up|answer|reject)\\b(?!\\s+(me|us)\\b)", RegexOptions.IgnoreCase);

	private static readonly Regex Hesitation = new Regex("\\b(do you want me to|you want me to|want me to|would you like me to|should i|shall i|is it ok(ay)? (if i|for me to|to)|ok(ay)? (if i|for me to|to)|mind if i|are you sure|you sure|just to (check|confirm)|(can|could) you confirm|confirm(ing)?|double[- ]?check(ing)?|ready (for me )?to)\\b[^.!?]{0,160}?\\b(send|sent|post|messag|dm|text|typ|writ|past|shar|repl|open|launch|start|clos|quit|join|leav|end|hang|call|ring|shut|turn|power|restart|reboot|sleep|lock|sign|log|delet|remov|click|press|mut|unmut|deafen|undeafen|play|paus|skip|search|install|uninstall|go ahead|proceed|do (it|that|this))\\w*|\\b(say the word|just say (yes|go|so|the word)|give me the (go[- ]ahead|ok|okay|word|green light)|(with|on) your (ok|okay|go[- ]ahead|say[- ]so)|let me know (if|when) (you want|you'?re ready|to go|it'?s ok))\\b|\\b(is that|that) (right|correct|ok(ay)?|what you want)\\s*\\?|^\\W*(are you sure|you sure|really|confirm)\\W*$", RegexOptions.IgnoreCase);

	private static readonly Regex Agreeing = new Regex("^(yes+|yeah|yea|yep|yup|ya|ye|sure|ok(ay)?|k|do it|go ahead|go for it|send it|post it|of course|obviously|duh|absolutely|definitely|correct|right|i'?m sure|i said (so|yes)|that'?s (right|it|what i said))\\b(?!.*\\b(but|except|instead|change|actually|wait|don'?t|not)\\b)", RegexOptions.IgnoreCase);

	private static readonly Regex Lead = new Regex("^((hey|yo|ok|okay|salut|bonjour|oye|hola)[\\s,]+)?(capri\\b[\\s,，、]*)?(((can|could|would|will) (you|u)|do (you|u)(?= mind)|please|pls|plz|stp|s'il te pla[iî]t|sil te pla[iî]t|(est-ce que )?tu peux|peux-tu|pourrais-tu|(me )?puedes|(me )?podr[ií]as|por favor|porfavor|porfa|bitte|per favore|perfavore|per piacere|proszę|prosze|пожалуйста|будь ласка|lütfen|lutfen|alsjeblieft|alstublieft|tolong)\\s+)*", RegexOptions.IgnoreCase);

	private static readonly Regex ScreenAsk = new Regex("^(?:(?:can|could|would|will|do|did) (?:you|u) )?(?:(?:take a |have a |quick )?(?:look|peek|peep|glance)(?: at)?|check(?: out)?|see|watch|read|scan|view|look over|screenshot)(?: on)? (?:my|the|our|this|ur|your) (?:screen|monitor|display|desktop|pc|computer|stream|window|tab)s?$|^(?:(?:can|could|would|will|do|did) (?:you|u) )?(?:see|look at|check(?: out)?|peep|watch) (?:this|that|what i'?m (?:doing|playing|watching|looking at|working on|seeing))$|^(?:look at|check out|check|peep|watch) (?:this|that)(?: (?:out|real quick|quick))?$|^(?:take a|have a|quick) (?:look|peek|glance)(?: at (?:this|that|it|my screen|the screen))?$|^what(?:'s| is) on (?:my|the|our) (?:screen|monitor|display|desktop|pc)\\b|^what (?:do|can|did) (?:you|u) see\\b|^what are (?:you|u) (?:seeing|looking at)$|^what am i (?:doing|playing|watching|looking at|working on|reading)(?: (?:on my (?:screen|pc|computer)|rn|right now|now))?$|^regardes? (?:mon [ée]cran|[çc]a)$|^(?:tu )?(?:peux )?(?:voir|vois) mon [ée]cran$|^tu vois (?:mon [ée]cran|[çc]a)$|^qu'est-ce que (?:je fais|tu vois)$|^qu'est-ce qu'il y a sur mon [ée]cran$|^(?:mira|revisa|checa|chequea|ve|ver|mirar|revisar|checar) (?:mi pantalla|esto|eso)$|^(?:ves|puedes ver) (?:mi pantalla|esto|eso)$|^qu[ée] (?:ves|estoy haciendo)(?: en (?:mi|la) pantalla)?$|^qu[ée] hay en (?:mi|la) pantalla$", RegexOptions.IgnoreCase);

	private static readonly Regex ScreenQuestion = new Regex("^what(?:'s| is) this(?: (?:game|app|thing|site|website|show|anime|movie|video|error|bug|code|program|word|map|level|boss|character|item|guy))?$|^what(?:'s| is) that (?:game|app|thing|site|website|show|anime|movie|video|error|bug|code|program|word|map|level|boss|character|item|guy)$|^what (?:game|app|anime|show|movie|video|website|site|error|program|level|map|boss|character|item) is (?:this|that)$|^who is this(?: (?:guy|character|boss))?$|^(?:how does|does) this look(?: (?:good|ok|okay|right|alright))?$|^what do (?:you|u) think (?:of|about) (?:this|my (?:screen|setup|build|base|drawing|art|design|avatar|character|thumbnail|outfit|fit|skin))$|^(?:look at|check out|peep|read) (?:this|that) \\w+(?: \\w+)?$|^rate (?:this|my (?:setup|desktop|wallpaper|screen|build|base|drawing|art|design|avatar|character|thumbnail|outfit|fit|skin))$", RegexOptions.IgnoreCase);

	private static readonly Regex Unseen = new Regex("\\b(can'?t|cannot|can not|don'?t|do not|unable to|not able to|no way to|have no way to)\\s+(\\w+\\s+){0,4}(see|look at|view|access|check|read)\\s+(your|ur|the|their)\\s+(screen|monitor|display|desktop)\\b|\\b(no|without)\\s+(access to|way to see|eyes on)\\s+(your|ur|the)\\s+screen\\b|\\b(share|show)\\s+(me\\s+)?(your|ur)\\s+screen\\b|\\bi('m| am)\\s+(blind|not looking at (your|the) screen)\\b", RegexOptions.IgnoreCase);

	private static readonly Regex Refusal = new Regex("\\bi (can'?t|cannot|can not|couldn'?t)\\s+(\\w+\\s+){0,2}(open|launch|start|run|play|pause|resume|skip|control|access|use|search|google|close|mute|unmute|put on|turn (up|down|on|off)|change)\\b|\\bi('?m| am) (unable|not able) to\\s+(\\w+\\s+){0,2}(open|launch|start|run|play|pause|resume|skip|control|access|use|search|google|close|mute|unmute|put on|turn (up|down|on|off)|change)\\b|\\bi (don'?t|do not) have (the )?(ability|permission|a way|any way) to\\s+(\\w+\\s+){0,2}(open|launch|start|run|play|pause|resume|skip|control|access|use|search|google|close|mute|unmute|put on|turn (up|down|on|off)|change)\\b|\\b(don'?t|do not|can'?t|cannot) (have )?(any )?access (to )?((your|ur|the|their)\\s+)?(apps?|spotify|youtube|discord|steam|music|pc|computer|programs?|browser|files|media player)\\b|\\bno (tengo|puedo) (acceso|abrir|reproducir|poner|controlar|acceder|usar|buscar|cerrar|tocar)\\b|\\bno tengo (forma|manera) de\\b|\\bje ne (peux|sais) pas (ouvrir|lancer|jouer|contr[oô]ler|acc[ée]der|utiliser|chercher|fermer|mettre)\\b|\\bje n'ai pas acc[eè]s\\b|\\b(don'?t|do not|can'?t|cannot) (have )?(any )?(control|access|ability)\\s+(of|over|to|on)\\s+((your|ur|the|their)\\s+)?(pc|computer|mouse|keyboard|cursor|screen|games?)\\b|\\bi (can'?t|cannot|can not) (move|click|press) (\\w+\\s+){0,2}(mouse|cursor|keys|keyboard|buttons?)\\b|\\bich kann (\\w+\\s+){0,3}nicht\\s+([öo]ffnen|abspielen|starten|steuern|spielen|zugreifen|bedienen|aufrufen)\\b|\\bkeinen? zugriff\\b|\\bn[ãa]o (consigo|posso|tenho como) (abrir|tocar|acessar|controlar|reproduzir|colocar|iniciar)\\b|\\bn[ãa]o tenho acesso\\b|\\bnon (posso|riesco a) (aprire|riprodurre|accedere|controllare|avviare|mettere|suonare)\\b|\\bnon ho accesso\\b|\\bnie (mog[ęe]|jestem w stanie) (otworzy[ćc]|odtworzy[ćc]|uruchomi[ćc]|w[łl][aą]czy[ćc]|pu[śs]ci[ćc])(?!\\w)|\\bnie mam dost[ęe]pu\\b|\\bik kan (\\w+\\s+){0,3}niet\\s+(openen|afspelen|starten|bedienen|spelen)\\b|\\bik heb geen toegang\\b|\\bне могу (открыть|включить|запустить|воспроизвести|поставить|получить доступ)(?!\\w)|\\bу меня нет доступа(?!\\w)|我(无法|無法|不能|没办法|沒辦法|没有办法|沒有辦法)(直接|帮你|幫你|为你|為你)?(打开|打開|开启|開啟|启动|啟動|播放|访问|訪問|存取|控制|运行|運行|使用|搜索|搜尋|关闭|關閉)|我(没有|沒有)(权限|權限|访问|訪問|存取)|(開く|開ける|起動する|再生する|操作する|アクセスする|検索する|閉じる)こと(が|は)?できません|(開け|起動でき|再生でき|操作でき|アクセスでき|検索でき|閉じられ)ません|アクセス(権|権限)?がありません|(열|켤|끌|닫을|틀|재생할|실행할|접근할|제어할|검색할|조작할) ?수 ?(가 ?)?없|접근 ?권한이 ?없|ไม\u0e48สามารถ(ท\u0e35\u0e48จะ)?(เป\u0e34ด|เล\u0e48น|เข\u0e49าถ\u0e36ง|ควบค\u0e38ม|ป\u0e34ด|ค\u0e49นหา)|ไม\u0e48ม\u0e35ส\u0e34ทธ\u0e34\u0e4c(ใน)?(การ)?เข\u0e49าถ\u0e36ง|\\b(mình|tôi|tớ|em) (không thể|ko thể|không|chẳng thể) (mở|bật|phát|truy cập|điều khiển|tắt|đóng|tìm)(?!\\w)|\\bkhông (có )?quyền truy cập(?!\\w)|\\b(aku|saya|gue|gw) (tidak|nggak|ga|gak|tak) (bisa|dapat|mampu) (membuka|buka|memutar|putar|menjalankan|mengakses|akses|mengontrol|mengendalikan|menutup|mencari|memainkan)\\b|\\btidak (punya|memiliki) akses\\b|\\b(açamıyorum|açamam|çalamıyorum|çalamam|oynatamıyorum|erişemiyorum|erişemem|kontrol edemiyorum|kapatamıyorum|başlatamıyorum)(?!\\w)|\\berişimim yok(?!\\w)|\\bне можу (відкрити|увімкнути|запустити|відтворити|поставити|отримати доступ)(?!\\w)|\\bу мене (немає|нема) доступу(?!\\w)|(لا|ما) (أستطيع|استطيع|يمكنني|أقدر|اقدر) (فتح|تشغيل|الوصول|التحكم|إغلاق|اغلاق|البحث)|ليس لدي (وصول|صلاحية|إمكانية)|لا (أملك|املك) (وصول|صلاحية)|नह\u0940\u0902 (ख\u094bल|चल\u093e|बज\u093e|ब\u0902द कर|क\u0902ट\u094dर\u094bल कर|एक\u094dस\u0947स कर) (सकत\u0940|सकत\u093e)|(एक\u094dस\u0947स|पह\u0941\u0902च|पह\u0941\u0901च) नह\u0940\u0902 ह\u0948|\\b(nahi|nahin) (khol|chala|baja|control kar) (sakti|sakta)\\b|\\bjag kan (\\w+\\s+){0,2}inte (öppna|spela|starta|styra|komma åt)(?!\\w)|\\bjag har ingen (åtkomst|tillgång)(?!\\w)|\\bjeg kan (\\w+\\s+){0,2}ikke (åpne|åbne|spille|starte|styre)(?!\\w)|\\bjeg har ikke (tilgang|adgang)(?!\\w)|\\ben (voi|pysty) (avaamaan|avata|soittamaan|soittaa|käynnistämään|käynnistää|hallitsemaan)(?!\\w)|\\bei ole pääsyä(?!\\w)|\\ben pääse käsiksi(?!\\w)|\\b(nemohu|nemůžu|nedokážu|nemôžem|nedokážem) (otevřít|pustit|spustit|přehrát|ovládat|otvoriť|pustiť|spustiť|prehrať|ovládať)(?!\\w)|\\bnemám (přístup|prístup)(?!\\w)|\\bnem tudom (megnyitni|elindítani|lejátszani|bezárni|irányítani)(?!\\w)|\\bnincs hozzáférésem(?!\\w)|\\bnem (tudok hozzáférni|férek hozzá)(?!\\w)|\\bnu pot (să |sa )?(deschid|deschide|pornesc|porni|reda|controla|închide|inchide)(?!\\w)|\\bnu am acces(?!\\w)|δεν μπορώ να (ανοίξω|βάλω|παίξω|ξεκινήσω|ελέγξω|κλείσω)|δεν έχω πρόσβαση|\\bhindi ko (kayang |magawang )?(mabuksan|buksan|mapatugtog|patugtugin|maisara|ma-?open|ma-?play)(?!\\w)|\\bwala akong access(?!\\w)|\\b(saya|aku) (tidak|tak) (boleh|dapat|mampu) (membuka|buka|memainkan|mainkan|memasang|pasang|mengakses|akses|mengawal|menutup|mencari)(?!\\w)|\\b(saya|aku) tiada akses(?!\\w)|\\bsiwezi (ku)?(fungua|cheza|funga|tafuta|washa|zima|dhibiti|fikia)(?!\\w)|\\bsina (ufikiaji|uwezo wa (ku)?(fungua|cheza|fikia))(?!\\w)|\\bno puc (obrir|reproduir|posar|engegar|controlar|accedir|tancar)(?!\\w)|\\bno tinc accés(?!\\w)|\\bne mogu (da )?(otvorim|pustim|pokrenem|kontrolišem|kontrolisem|otvoriti|pustiti|pokrenuti|kontrolirati|pristupiti)(?!\\w)|\\bnemam pristup(?!\\w)|\\bне могу (да )?(отворим|пустим|покренем|контролишем)(?!\\w)|\\bнемам приступ(?!\\w)|\\bei (saa|suuda) (ma |mina )?(\\w+ ){0,2}(avada|mängida|käivitada|juhtida|sulgeda)(?!\\w)|\\bmul (pole|ei ole) (juurdepääsu|ligipääsu)(?!\\w)|\\bnevaru (\\w+ ){0,2}(atvērt|atskaņot|palaist|piekļūt|kontrolēt|ieslēgt|aizvērt)(?!\\w)|\\bman nav piekļuves(?!\\w)|\\bnegaliu (\\w+ ){0,2}(atidaryti|paleisti|groti|valdyti|pasiekti|įjungti|uždaryti)(?!\\w)|\\bneturiu prieigos(?!\\w)|\\bne morem (\\w+ ){0,2}(odpreti|predvajati|zagnati|upravljati|dostopati|zapreti)(?!\\w)|\\bnimam dostopa(?!\\w)|\\bне мога да (отворя|пусна|включа|контролирам|стартирам|достъпя|затворя)(?!\\w)|\\bнямам достъп(?!\\w)|\\bma (nistax|nistgħax) (niftaħ|niftah|indoqq|nikkontrolla|nidħol|nixgħel|nagħlaq)(?!\\w)|\\bm'?għandix aċċess(?!\\w)|אני לא (יכולה|יכול|מסוגלת|מסוגל) ל(פתוח|נגן|הפעיל|שלוט|גשת|סגור|חפש)|אין לי גישה|نہیں (کھول|چلا|بجا|بند کر|کنٹرول کر|ایکسیس کر|رسائی حاصل کر) (سکتی|سکتا)|(رسائی|ایکسیس) (نہیں ہے|حاصل نہیں)|میرے پاس (رسائی|ایکسیس) نہیں|نمی[\\u200C ]?(تونم|توانم) .{0,40}?(باز کنم|پخش کنم|اجرا کنم|کنترل کنم|ببندم|بذارم|بگذارم)|دسترسی (ندارم|نداریم)|(খ\u09c1লত\u09c7|চ\u09beল\u09beত\u09c7|ব\u09beজ\u09beত\u09c7|বন\u09cdধ করত\u09c7|ন\u09bfয\u09bcন\u09cdত\u09cdরণ করত\u09c7|অ\u09cdয\u09beক\u09cdস\u09c7স করত\u09c7) (প\u09beরব ন\u09be|প\u09beরব\u09cb ন\u09be|প\u09beরছ\u09bf ন\u09be|প\u09beর\u09bf ন\u09be|প\u09beরবন\u09be|প\u09beরছ\u09bfন\u09be|প\u09beর\u09bfন\u09be)|(অ\u09cdয\u09beক\u09cdস\u09c7স|প\u09cdরব\u09c7শ\u09beধ\u09bfক\u09beর) ন\u09c7ই|(उघड\u0942|ल\u093eव\u0942|च\u093eल\u0942 कर\u0942|ब\u0902द कर\u0942|न\u093fय\u0902त\u094dर\u093fत कर\u0942) शकत न\u093eह\u0940|(ऍक\u094dस\u0947स|ॲक\u094dस\u0947स|एक\u094dस\u0947स|प\u094dरव\u0947श) न\u093eह\u0940|(ખ\u0acbલ\u0ac0|વગ\u0abeડ\u0ac0|ચ\u0abeલ\u0ac1 કર\u0ac0|બ\u0a82ધ કર\u0ac0|ન\u0abfય\u0a82ત\u0acdર\u0abfત કર\u0ac0) (શકત\u0ac0|શકત\u0acb|શક\u0ac1\u0a82) નથ\u0ac0|(ઍક\u0acdસ\u0ac7સ|એક\u0acdસ\u0ac7સ|પ\u0acdરવ\u0ac7શ) નથ\u0ac0|ਨਹ\u0a40\u0a02 (ਖ\u0a4bਲ\u0a4dਹ|ਖ\u0a4bਲ|ਚਲ\u0a3e|ਵਜ\u0a3e|ਬ\u0a70ਦ ਕਰ|ਕ\u0a70ਟਰ\u0a4bਲ ਕਰ) (ਸਕਦ\u0a40|ਸਕਦ\u0a3e)|(ਐਕਸ\u0a48ਸ|ਐਕਸ\u0a48\u0a71ਸ|ਪਹ\u0a41\u0a70ਚ) ਨਹ\u0a40\u0a02|(த\u0bbfறக\u0bcdக|இயக\u0bcdக|ம\u0bc2ட|கட\u0bcdட\u0bc1ப\u0bcdபட\u0bc1த\u0bcdத|அண\u0bc1க|ப\u0bcbட) (ம\u0bc1ட\u0bbfய\u0bbeத\u0bc1|இயல\u0bbeத\u0bc1)|அண\u0bc1கல\u0bcd இல\u0bcdல\u0bc8|(త\u0c46రవ|ప\u0c4dల\u0c47 చ\u0c47య|న\u0c3fయ\u0c02త\u0c4dర\u0c3f\u0c02చ|య\u0c3eక\u0c4dస\u0c46స\u0c4d చ\u0c47య|మ\u0c42స\u0c3fవ\u0c47య)ల\u0c47న\u0c41|య\u0c3eక\u0c4dస\u0c46స\u0c4d ల\u0c47ద\u0c41|(ತ\u0cc6ರ\u0cc6ಯಲ\u0cc1|ಪ\u0ccdಲ\u0cc7 ಮ\u0cbeಡಲ\u0cc1|ನ\u0cbfಯ\u0c82ತ\u0ccdರ\u0cbfಸಲ\u0cc1|ಪ\u0ccdರವ\u0cc7ಶ\u0cbfಸಲ\u0cc1|ಮ\u0cc1ಚ\u0ccdಚಲ\u0cc1) (ಸ\u0cbeಧ\u0ccdಯವ\u0cbfಲ\u0ccdಲ|ಸ\u0cbeಧ\u0ccdಯವ\u0cbeಗ\u0cc1ವ\u0cc1ದ\u0cbfಲ\u0ccdಲ|ಆಗ\u0cc1ವ\u0cc1ದ\u0cbfಲ\u0ccdಲ|ಆಗಲ\u0ccdಲ)|ಆಕ\u0ccdಸ\u0cc6ಸ\u0ccd ಇಲ\u0ccdಲ|(ത\u0d41റക\u0d4dക\u0d3eൻ|പ\u0d4dല\u0d47 ച\u0d46യ\u0d4dയ\u0d3eൻ|ന\u0d3fയന\u0d4dത\u0d4dര\u0d3fക\u0d4dക\u0d3eൻ|ആക\u0d4dസസ\u0d4d ച\u0d46യ\u0d4dയ\u0d3eൻ|അടയ\u0d4dക\u0d4dക\u0d3eൻ) (കഴ\u0d3fയ\u0d3fല\u0d4dല|പറ\u0d4dറ\u0d3fല\u0d4dല|സ\u0d3eധ\u0d3fക\u0d4dക\u0d3fല\u0d4dല)|ആക\u0d4dസസ\u0d4d ഇല\u0d4dല|\\bnon (podo|son quen de) (abrir|reproducir|poñer|poner|acceder|controlar|pechar|iniciar)(?!\\w)|\\bnon teño acceso(?!\\w)|\\bezin (dut|dizut) (\\w+ ){0,3}(ireki|erreproduzitu|jarri|itxi|kontrolatu|bilatu|piztu|itzali|abiarazi)(?!\\w)|\\b(ireki|erreproduzitu|jarri|itxi|kontrolatu|piztu) ezin (dut|dizut)(?!\\w)|\\bez (dut|daukat) sarbiderik(?!\\w)|\\bek kan nie (\\w+\\s+){0,3}(oopmaak|speel|afspeel|beheer|toemaak|aansit|begin|bykom)(?!\\w)|\\bek kan (\\w+\\s+){0,3}nie (oopmaak|speel|afspeel|beheer|toemaak|aansit|begin|bykom)(?!\\w)|\\bek het (nie|geen) toegang(?!\\w)|\\b(ég get|get ég) (ekki|ei) (\\w+ ){0,2}(opnað|opnad|spilað|spilad|stjórnað|stjornad|lokað|lokad|ræst|raest|nálgast|nalgast)(?!\\w)|\\b(ég hef|hef ég) ekki aðgang(?!\\w)|\\bnuk (mund|mundem|jam në gjendje|jam ne gjendje) (të |te |ta |t'i )?(hap|luaj|mbyll|kontrolloj|aksesoj|nis|ndez|vë|ve)(?!\\w)|\\bnuk kam (qasje|akses)(?!\\w)|\\bní (féidir|feidir|thig) liom (\\w+ ){0,3}(a )?(oscailt|sheinm|sheinnt|rialú|rialu|dhúnadh|dhunadh|rochtain|chur ar siúl)(?!\\w)|\\bníl rochtain agam(?!\\w)|\\b(dw i ddim yn gallu|dydw i ddim yn gallu|alla i ddim|fedra i ddim|ni allaf) (\\w+ ){0,2}(agor|chwarae|rheoli|cau|cyrchu|cael mynediad|dechrau)(?!\\w)|\\b(does gen i ddim|does dim) mynediad(?!\\w)|\\bне магу (адкрыць|уключыць|ўключыць|запусціць|прайграць|паставіць|атрымаць доступ|зачыніць)(?!\\w)|\\bу мяне няма доступу(?!\\w)|\\bне можам (да )?(го |ја |ги )?(отворам|пуштам|вклучам|контролирам|стартувам|пристапам|затворам)(?!\\w)|\\bнемам пристап(?!\\w)|\\b(aça|oxuda|qoşa|işə sala|ise sala|idarə edə|idare ede|bağlaya|baglaya) bilmirəm(?!\\w)|\\b(girişim|çıxışım|icazəm) yoxdur(?!\\w)|\\b(ocha|ochib bera|ijro eta|boshqara|yopa|qo['ʻ‘`]?ya|ishga tushira) olmayman(?!\\w)|\\b(kirish|ruxsat|ruxsatim|kirishim) yo['ʻ‘`]?q(?!\\w)|(аша|ойната|қоса|басқара|жаба|іске қоса) алмаймын(?!\\w)|(қолжетімділігім|рұқсатым|кіру мүмкіндігім) жоқ(?!\\w)|(нээж|тоглуулж|асааж|удирдаж|хааж|хандаж) (чадахгүй|чадсангүй)|(хандах эрх|хандалт) (байхгүй|алга)|(ख\u094bल\u094dन|बज\u093eउन|चल\u093eउन|बन\u094dद गर\u094dन|न\u093fयन\u094dत\u094dरण गर\u094dन|पह\u0941\u0901च गर\u094dन) (सक\u094dद\u093fन\u0901|सक\u094dद\u093fन|सक\u094dद\u093f\u0901न|सक\u094dत\u093fन\u0901)|पह\u0941\u0901च छ\u0948न|\\b(aku|kula) (ora|mboten) (iso|isa|bisa|saged) (mbukak|bukak|mbikak|muter|nyetel|ngakses|ngontrol|nutup|nggoleki|nglakokake)(?!\\w)|\\b(aku|kula) (ora|mboten) (duwe|nduwe|gadhah) akses(?!\\w)|\\bba (zan|na) iya (buɗe|bude|kunna|rufe|sarrafa|samun dama)(?!\\w)|\\bba ni da (damar|izinin|ikon)(?!\\w)|\\b(mi ò lè|mi o le|mi kò lè|mi ko le|n kò lè|n ko le|èmi kò lè|emi ko le) (ṣí|ṣi|si|ṣe|se|ṣàkóso|sakoso|wọlé|wole|pa)(?!\\w)|\\b(enweghị m ike|enweghi m ike|apụghị m|apughi m) (imeghe|imepe|ịkpọ|ikpo|imechi|ịchịkwa|ichikwa|ịbanye|ibanye)(?!\\w)|\\benweghị m (ohere|ikike)(?!\\w)|\\b(angikwazi|andikwazi|andinako) uku(vula|dlala|vala|lawula|finyelela|fikelela|qala|cisha|qalisa)(?!\\w)|\\b(anginayo|andinayo|andinalo) (imvume|ukufinyelela|ufikelelo)(?!\\w)|\\bma (awoodo|kari karo|karo) (inaan )?(furo|daaro|shido|xiro|xidho|maamulo|galo|bilaabo)(?!\\w)|\\bma (furi|daari|shidi|xiri|xidhi|maamuli|gali|bilaabi) karo(?!\\w)|\\bma haysto (oggolaansho|fursad|marin)(?!\\w)|(መክፈት|ማጫወት|መዝጋት|መቆጣጠር|መድረስ|ማስጀመር|መፈለግ) (አልችልም|አልቻልኩም)|(መዳረሻ|ፈቃድ|ፍቃድ) የለኝም|ບ\u0ecd\u0ec8ສາມາດ ?(ເປ\u0eb5ດ|ຫ\u0ebc\u0eb4\u0ec9ນ|ຫລ\u0eb4\u0ec9ນ|ເຂ\u0ebb\u0ec9າເຖ\u0eb4ງ|ຄວບຄ\u0eb8ມ|ປ\u0eb4ດ|ຊອກຫາ)|ບ\u0ecd\u0ec8ມ\u0eb5ສ\u0eb4ດ ?(ເຂ\u0ebb\u0ec9າເຖ\u0eb4ງ|ເຂ\u0ebb\u0ec9າໃຊ\u0ec9)|ម\u17b7ន ?អ\u17b6ច ?(ប\u17beក|ច\u17b6ក\u17cb|ច\u17bcលប\u17d2រ\u17be|គ\u17d2រប\u17cbគ\u17d2រង|ប\u17b7ទ|ស\u17d2វ\u17c2ងរក)|(គ\u17d2ម\u17b6ន|ម\u17b7នម\u17b6ន) ?ស\u17b7ទ\u17d2ធ\u17b7 ?(ច\u17bcលប\u17d2រ\u17be|ច\u17bcល)|မ(ဖ\u103dင\u1037\u103a|ပ\u102dတ\u103a|ရ\u103e\u102c|ထ\u102dန\u103a\u1038ခ\u103b\u102fပ\u103a|ဝင\u103aရ\u1031\u102cက\u103a)(ပ\u1031\u1038)?န\u102d\u102fင\u103a|(ဖ\u103dင\u1037\u103a|ပ\u102dတ\u103a|ရ\u103e\u102c|ထ\u102dန\u103a\u1038ခ\u103b\u102fပ\u103a|ဝင\u103aရ\u1031\u102cက\u103a)(ပ\u1031\u1038)?(လ\u102d\u102f\u1037|၍) ?မရ|(ဝင\u103aရ\u1031\u102cက\u103aခ\u103dင\u1037\u103a|ဝင\u103aခ\u103dင\u1037\u103a|အသ\u102f\u1036\u1038ပ\u103c\u102fခ\u103dင\u1037\u103a|ခ\u103dင\u1037\u103aပ\u103c\u102fခ\u103bက\u103a) ?မရ\u103e\u102d|ვერ (გავხსნი|ვხსნი|ჩავრთავ|ვრთავ|დავუკრავ|ვუკრავ|ვაკონტროლებ|ვმართავ|დავხურავ|მოვძებნი|შევალ)(?!\\w)|არ შემიძლია (\\S+ ){0,2}(გახსნა|ჩართვა|დაკვრა|კონტროლი|მართვა|დახურვა)(?!\\w)|(გახსნა|ჩართვა|დაკვრა|კონტროლი|მართვა|დახურვა) არ შემიძლია|წვდომა არ მაქვს|არ მაქვს წვდომა|չեմ կարող (\\S+ ){0,2}(բացել|միացնել|նվագել|կառավարել|փակել|անջատել|մուտք գործել)(?!\\w)|(բացել|միացնել|նվագել|կառավարել|փակել|անջատել) չեմ կարող|(հասանելիություն|մուտք) չունեմ|չունեմ (հասանելիություն|մուտք)", RegexOptions.IgnoreCase);

	private static readonly Regex MouthOrEyes = new Regex("\\b(close|shut) (your|ur|ya|that) (damn |fucking )?(mouth|trap|gob|eyes)\\b|\\bopen (your|ur|ya) (eyes|mind|heart|mouth)\\b|\\bcierra (la|tu|esa) boca\\b|\\bcierra el pico\\b|\\b(cierra|abre) (los|tus) ojos\\b|\\bferme(-la\\b|[- ]la\\s*[.!?]*$|[- ]ta (gueule|bouche)\\b|[- ]ton clapet\\b|[- ](les|tes) yeux\\b)|\\bouvre (les|tes) yeux\\b|\\bfecha (a|essa|tua|sua) (boca|matraca)\\b|\\b(fecha|abre) (os|teus|seus) olhos\\b|\\bchiudi (la|quella|sta|la tua) bocca\\b|\\bchiudi il becco\\b|\\b(chiudi|apri) gli occhi\\b|\\bmach(e)? (die|deine) augen (auf|zu)\\b|\\bmach(e)? (den|deinen) mund (auf|zu)\\b|\\b(schlie(ß|ss)e?|[öo]ffne) (die|deine) augen\\b|\\bzamknij (się|sie|mordę|morde|japę|jape|ryj|buzię|buzie|dziób|dziob|pysk|gębę|gebe|oczy)(?!\\w)|\\botw[óo]rz oczy\\b|(?<!\\w)(закрой|закрий|зачыні|затвори) (свой |свій |си )?(рот|рота|пасть|хлебало|варежку|пельку|писок|уста|устата|губицу|глаза|очі|вочы|очи)(?!\\w)|(?<!\\w)(открой|відкрий|адкрый|отвори) (глаза|очі|вочы|очи)(?!\\w)|\\b(zatvori|zapri) (usta|gubicu|labrnju|gobec|oči|oci)\\b|\\botvori (oči|oci)\\b|\\btutup (mulut|mulutmu|mata|matamu)\\b|\\bbuka (mata|matamu)\\b|\\btutupen cangkem(mu)?\\b|\\bg[öo]zlerini (kapat|aç)\\b|\\bağzını bağla\\b|\\bgözlərini (bağla|aç)\\b|\\bog['ʻ‘`]?zingni yop\\b|\\bko['ʻ‘`]?zingni (yop|och)\\b|аузыңды жап|\\bstäng (munnen|käften|truten|ögonen)\\b|\\böppna ögonen\\b|\\blukk (munnen|kjeften|trynet|øynene)\\b|\\båpne øynene\\b|\\bluk (munden|kæften|øjnene)\\b|\\båbn øjnene\\b|\\bsulje (suusi|turpasi|silmäsi)\\b|\\bavaa silmäsi\\b|\\bzavři (hubu|pusu|zobák|tlamu|klapačku|oči)\\b|\\botevři oči\\b|\\bzatvor (hubu|ústa|zobák|oči)\\b|\\botvor oči\\b|\\bzárd be a (szád|szádat|pofád|pofádat|szemed)\\b|\\bnyisd ki a szemed\\b|\\b[îi]nchide(-[țt]i)? (gura|pliscul|ochii)\\b|\\bdeschide(-[țt]i)? ochii\\b|κλε[ίι]σε το στ[όο]μα|κλε[ίι]σε τα μ[άα]τια|[άα]νοιξε τα μ[άα]τια|\\bisara (mo )?(ang )?(bibig|mata)\\b|\\bbuksan (mo )?(ang )?mata\\b|\\bfunga (mdomo|domo|macho)\\b|\\bfungua macho\\b|\\btanca (la boca|el bec|els ulls)\\b|\\bobre els ulls\\b|\\bpecha (a boca|os ollos)\\b|\\bitxi (ahoa|muturra|begiak)\\b|\\bireki begiak\\b|\\bmaak (jou )?(bek|mond|smoel|oë) toe\\b|\\bmaak (jou )?oë oop\\b|\\bloka(ð|d)u (munninum|kjaftinum|augunum)\\b|\\bmbylle goj[ëe]n\\b|\\bmbylli syt[ëe]\\b|\\bhape syt[ëe]\\b|\\bdún do (bhéal|shúile)\\b|\\bcae(a|wch) (dy|eich) (geg|ceg|llygaid)\\b|\\buždaryk (burną|burna|snukį|akis)\\b|\\baizver (muti|purnu|acis)\\b|\\batver acis\\b|\\bpane (suu|silmad) kinni\\b|\\bagħlaq (ħalqek|għajnejk)\\b|\\baghlaq (halqek|ghajnejk)\\b|\\brufe (bakin|idanu)|\\bmechie (ọnụ|onu|anya)|\\bvala (umlomo|amehlo)\\b|\\bđóng (mồm|miệng|mõm)\\b|\\bmở mắt\\b|\\b(muh|munh|mooh|muhh|mu) band\\b|\\baankhen (band|kholo)\\b|(סגור|סגרי|תסגור|תסגרי) את (הפה|העיניים)|(פתח|פתחי|תפתח|תפתחי) את העיניים|(سكر|سك\u0651ر|اغلق|أغلق|إغلق|سد|سد\u0651) (تمك|فمك|بقك|نيعك|عيونك|عينيك)|(افتح|إفتح) (عيونك|عينيك)|(دهنتو|دهنت رو|دهانت را|دهنت|چشماتو|چشمانت را) ببند|چشماتو باز کن|منہ بند|آنکھیں (بند|کھولو)|(म\u0941\u0902ह|म\u0941\u0901ह) ब\u0902द|म\u0941ख बन\u094dद|त\u094b\u0902ड ब\u0902द|(आ\u0902ख\u0947\u0902|आ\u0901ख\u0947\u0902) (ब\u0902द|ख\u094bल\u094b)|ম\u09c1খ বন\u09cdধ|চ\u09cbখ (বন\u09cdধ|খ\u09cbল\u09cb)|મ\u0acbઢ\u0ac1\u0a82 બ\u0a82ધ|આ\u0a82ખ\u0acb બ\u0a82ધ|ਮ\u0a42\u0a70ਹ ਬ\u0a70ਦ|ਅ\u0a71ਖ\u0a3e\u0a02 ਬ\u0a70ਦ|வ\u0bbeய\u0bc8 ம\u0bc2ட\u0bc1|(கண\u0bcdகள\u0bc8|கண\u0bcdண\u0bc8) ம\u0bc2ட\u0bc1|ಬ\u0cbeಯ\u0cbf ಮ\u0cc1ಚ\u0ccdಚ\u0cc1|ಕಣ\u0ccdಣ\u0cc1 ಮ\u0cc1ಚ\u0ccdಚ\u0cc1|വ\u0d3eയ അടയ\u0d4dക\u0d4dക|കണ\u0d4dണ\u0d4d ?അടയ\u0d4dക\u0d4dക|አፍህን ዝጋ|አፍሽን ዝጊ|አፋችሁን ዝጉ|ប\u17b7ទម\u17b6ត\u17cb|ប\u17b7ទភ\u17d2ន\u17c2ក|ປ\u0eb4ດປາກ|ປ\u0eb4ດຕາ(?!ຕະລາງ|ມ(?!\u0eb1)|ຍ(?!\u0eb1))|ป\u0e34ดปาก|ป\u0e34ดตา(?!ราง|ม(?!\u0e31)|ย(?!\u0e31)|ก(?![\u0e48\u0e31]))|เป\u0e34ดตา(?!ราง|ม(?!\u0e31)|ย(?!\u0e31)|ก(?![\u0e48\u0e31]))|目を(閉じて|とじて|開けて|あけて)|口を(閉じて|とじて)|입 ?(닫아|다물어)|բերանդ փակիր|աչքերդ (փակիր|բացիր)|မ\u103bက\u103aလ\u102f\u1036\u1038 ?(ပ\u102dတ\u103a|ဖ\u103dင\u1037\u103a)", RegexOptions.IgnoreCase);

	private static readonly Regex Chatter = new Regex("^(play (with|along|around|dead|dumb|nice|fair|a game with|a joke)|open up (to|about)|start (a story|a joke|an argument)|run (a mile|a race)|kill (the boss|me|time)|look (at it this way|forward)|show me (how|why)|check my (spelling|grammar)|rate my joke|mets-toi)\\b", RegexOptions.IgnoreCase);

	private static readonly Regex Napping = new Regex("^(?:(?:go (?:back )?to (?:sleep|bed)|(?:go )?(?:take|have|grab) (?:a |ur |your )?(?:little |quick |power )?(?:nap|snooze|rest)|go (?:nap|rest|snooze|sleep)|nap(?: time)?|sleep|doze off|get some (?:sleep|rest))(?: (?:here|there|right here|right there|over here|where you are))?|(?<wake>wake(?: up|y wakey)|rise and shine))(?:[\\s,]+(?:now|then|already|girl|choom|[ck]app?r(?:i|ee|ie|y)))*$", RegexOptions.IgnoreCase);

	internal static readonly Regex TaskbarWords = new Regex("^(?:(?:the|my|your|ur) )?(?:task ?bar|floor|ground|bottom of (?:the|my) screen)$", RegexOptions.IgnoreCase);

	internal static readonly Regex MainframeWords = new Regex("^(?:(?:the|my|your|ur|capri'?s?) )?(?:capri )?main ?frame(?: window)?$", RegexOptions.IgnoreCase);

	private static readonly string[] SelfWords = new string[40]
	{
		"^(?:move|put|bring|send|drag|throw|toss|shift|teleport|switch|take|push)(?: (?:the|my|your|ur|capri'?s?|her))?(?: capri)? main ?frame(?: window| app)?(?: (?:over|back|across))?(?: (?:to|onto|on|into|over to))? (?:the |my |your |ur )?(?:(?<s>other|next|another|second|2nd|third|3rd|fourth|4th|first|1st|left|right|top|bottom|upper|lower|main|primary|previous|last)(?: one)? ?(?:monitor|screen|display)s?|(?:monitor|screen|display) (?:number |no\\.? ?|#)?(?<s>\\d|one|two|three|four))(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "move_mainframe", "^(?:move|put|bring|send|drag|teleport|take|pull)(?: (?:the|my|your|ur|capri'?s?|her))?(?: capri)? main ?frame(?: window| app)?(?: (?:over|back|up))? (?<s>here|over here|to me|to (?:my|the) (?:mouse|cursor|pointer)|to this (?:monitor|screen|display))(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "move_mainframe", "^(?:move|switch|swap)(?: (?:the|my|your|ur|capri'?s?|her))?(?: capri)? main ?frame(?: window| app)?(?: (?:over|across))?(?: (?:monitors|screens|displays))?(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "move_mainframe", "^(?:teleport|tp|warp|glitch|zap|beam|go|move|hop|jump|switch|head|pop|run|walk|fly|float|get|come)(?: (?:yourself|urself|your ass|ur ass))?(?: (?:over|back|across))?(?: (?:to|onto|on|into|over to|across to))? (?:the |my |your |ur |our )?(?:(?<s>other|next|another|second|2nd|third|3rd|fourth|4th|first|1st|left|right|top|bottom|upper|lower|main|primary|previous|last)(?: one)? ?(?:monitor|screen|display)s?|(?:monitor|screen|display) (?:number |no\\.? ?|#)?(?<s>\\d|one|two|three|four))(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "other_screen", "^(?:switch|swap|change) (?:monitors|screens|displays)(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "other_screen",
		"^(?:(?:teleport|tp|warp|glitch|zap|beam|come|get|hop|pop|jump|move|go|run|walk|fly|float)(?: (?:yourself|urself|your ass|ur ass))?(?: (?:on|over|back|right))? (?:here|over here|to me|over to me|(?:to|by|next to) (?:my|the) (?:mouse|cursor|pointer)|(?:(?:over )?to |over )?where (?:my|the) (?:mouse|cursor|pointer) is|next to me|to this (?:monitor|screen|display))|come here|c'?mere|get over here|come (?:back|over)(?: here)?|come (?:back )?to me|teleport|tp)(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "come_here", "^(?:sit(?: (?:down|back down))?(?: (?:here|there|right here|right there|where you are))?|take a seat|have a seat|park it|sit still)(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "sit", "^(?:stand(?: (?:up|back up))?|get up|let go|on your feet)(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "stand_up", "^(?:hang|dangle)(?: (?:here|there|right here|right there|in the air|in mid ?air|where you are))?(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "hang", "^(?:ride|hop on|get on|jump on|sit on|climb on|ride on)(?: (?:my|the|ur|your))? (?:mouse|cursor|pointer)(?: again)?(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "ride_cursor",
		"^(?:(?:hop|get|jump|climb) off(?: (?:of|from))?(?: (?:my|the|ur|your))? (?:mouse|cursor|pointer)|stop riding(?: (?:my|the|ur|your) (?:mouse|cursor|pointer))?|(?:let go of|leave) (?:my|the) (?:mouse|cursor|pointer))(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "hop_off", "^(?:hide(?: (?:yourself|urself|away))?|go (?:and )?hide|disappear|vanish|go invisible|turn invisible|poof)(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "hide", "^(?:show (?:yourself|urself)|un-?hide(?: (?:yourself|urself))?|reappear|appear|come out(?: of hiding)?|stop hiding|show up)(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "show", "^(?:(?:go|come|now|please|pls|plz) )?(?:(?:lie|lay)(?: (?:yourself|urself))?(?: back)? down(?: (?:for a (?:bit|while|minute|sec)|and (?:rest|relax|chill)))?(?: (?:here|there|right here|right there|where you are|on (?:it|that|this|the window|this window|that window|your window|the ledge)))?|(?:lie|lay) (?:back|flat)|lie on your back|(?:take|have) a lie ?down)(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "lie_down", "^(?:(?:go|come|now|please|pls|plz) )?sit (?:back )?up(?: straight)?(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "sit_up",
		"^(?:(?:go|come|now|please|pls|plz) )?(?:let'?s )?(?:(?:walk|stroll|wander|roam|pace)(?: (?:around|about|a bit|a little|for a bit|for a while|back and forth|up and down))*|(?:go )?(?:for|on) a (?:little |short |quick )?(?:walk|stroll)|take a (?:little |short |quick )?(?:walk|stroll)|stretch your legs)(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "walk_around", "^(?:(?:go|come|now|please|pls|plz) )?(?:teleport|tp|warp|glitch|zap|beam|hop|jump|go|move|pop|fly|float|run)(?: (?:yourself|urself|your ass|ur ass))?(?: (?:off|away))?(?: (?:to|over to|off to))? (?:somewhere(?: else| random| new)?|anywhere(?: else)?|a random (?:spot|place|window)|some ?place (?:else|random|new)|elsewhere|around)(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "wander", "^(?:(?:go|come|now|please|pls|plz) )?(?:sit|perch|plop|park|settle)(?: (?:down|yourself|urself|your ass|ur ass|back down))? (?:on|onto|on top of|up on|atop|over on) (?!(?:(?:the|my|your|ur|our|that|this) )?(?:it|that|this|there|here|me|you|him|her|them|us|lap|face|head|knees?|bed|couch|sofa|chair|cursor|mouse|pointer|window|ledge|back|side|stomach|tummy)\\b)(?:the |my |your |ur |our |that |this )?(?<w>[\\p{L}\\p{Nd}][\\p{L}\\p{Nd} .'&+-]{0,40}?)(?: window| app| game| tab)?(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "sit_on", "^(?:(?:go|come|now|please|pls|plz) )?(?:hop|jump|climb|get|move|teleport|tp|warp|glitch|fly|float)(?: (?:yourself|urself|your ass|ur ass))?(?: (?:over|up|back))?(?: (?:on|onto|on top of|up on|to|over to))? (?!(?:the |my |your |ur |our )?(?:other|next|another|second|third|left|right|top|bottom|main|primary)\\b)(?!(?:(?:the|my|your|ur|our|that|this) )?(?:it|that|this|there|here|me|you|him|her|them|us|lap|face|head|knees?|bed|couch|sofa|chair|cursor|mouse|pointer|window|ledge|back|side|stomach|tummy)\\b)(?:the |my |your |ur |our |that |this )?(?<w>[\\p{L}\\p{Nd}][\\p{L}\\p{Nd} .'&+-]{0,40}?) window(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "sit_on", "^(?:(?:go|come|now|please|pls|plz) )?(?:lie|lay)(?: (?:yourself|urself))?(?: back)?(?: down)? (?:on|onto|on top of|along|across) (?!(?:(?:the|my|your|ur|our|that|this) )?(?:it|that|this|there|here|me|you|him|her|them|us|lap|face|head|knees?|bed|couch|sofa|chair|cursor|mouse|pointer|window|ledge|back|side|stomach|tummy)\\b)(?:the |my |your |ur |our |that |this )?(?<w>[\\p{L}\\p{Nd}][\\p{L}\\p{Nd} .'&+-]{0,40}?)(?: window| app| game| tab)?(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "lie_down"
	};

	private static readonly string[] AnimationWords = new string[14]
	{
		"^(?:stop|quit|no more|enough|that'?s enough|cut the|cut out the) (?:dancing|the dance|your dance|ur dance|the dancing|grooving|boogying)(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "stop_dancing", "^(?:dance|let'?s dance|start dancing|keep dancing|go dance|bust a move|show (?:me|us) (?:your|ur|some) (?:dance )?moves|do (?:a|the|your|ur|some|that) (?:little |lil |happy |victory )?(?:dance|jig|boogie)|dance (?:with me|to (?:it|this|that|the music|the song|the beat|my music|my song)|again|some more|a bit|a little))(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "dance", "^(?:glitch(?: out)?|do (?:a|your|ur|the|that) (?:little )?glitch(?: thing)?|do the glitchy thing)(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "glitch", "^(?:grin|smile|smirk|do (?:a|your|ur|the|that) (?:little |big )?(?:grin|smile|smirk)|give (?:me|us) (?:a |your |ur |that )?(?:little |big )?(?:grin|smile)|smile (?:for (?:me|us|the camera)|at me))(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "grin", "^(?:blink(?: (?:once|twice|at me))?|do (?:a|your|ur) (?:little )?blink)(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "blink",
		"^(?:idle|go idle|be idle|stand still|stay still|stop moving|(?:go )?back to (?:normal|idle))(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", "idle", "^(?:(?:play|do|show me|show|run|perform|start|go)(?: (?:your|ur|the|a|an|that))? )?(?<n>idle|glitch|grin|smile|blink|dance|dancing)(?: (?:animation|anim|animations|clip|move|moves|loop))(?:[\\s,]+(?:now|then|already|again|quick|real quick|for me|for us|please|pls|plz|girl|choom|bestie|[ck]app?r(?:i|ee|ie|y)))*$", ""
	};

	private static readonly string[] MediaWords = new string[111]
	{
		"^(?:(?:set|put|turn|change|make|bring|get|adjust|move|crank|drop|lower|raise|have)\\s+)?(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|speakers?)(?:\\s+level)?\\s+(?:to|at|on|=)?\\s*(?<n>\\d{1,3})\\s*(?:%|percent|per ?cent|por ?ciento|pour ?cent)?$", "volume_set", "n", "^(?:set|put|turn|bring|crank|drop|lower|raise|move)\\s+(?:it|this|that)\\s+(?:(?:up|down)\\s+)?(?:to|at)\\s+(?<n>\\d{1,3})\\s*(?:%|percent|per ?cent)$", "volume_set", "n", "^(?:turn|crank|bring|put)\\s+(?:it|this|that|(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|song|track|tune|tunes|video|game|pc|computer|laptop|speakers?|headphones?|headset|everything|spotify|youtube))\\s+(?:up|down)\\s+to\\s+(?<n>\\d{1,3})\\s*(?:%|percent|per ?cent|por ?ciento|pour ?cent)?$", "volume_set", "n", "^(?:max|maximi[sz]e|full)\\s+(?:out\\s+)?(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|speakers?)(?:\\s+out)?$|^(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|speakers?)\\s+(?:to\\s+|at\\s+|on\\s+)?(?:the\\s+)?(?:max|maximum|full|top)$|^(?:full|max|maximum)\\s+volume$|^(?:turn|crank|pump|put)\\s+(?:it|this|that|(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|song|track|tune|tunes|video|game|pc|computer|laptop|speakers?|headphones?|headset|everything|spotify|youtube))\\s+(?:all the way up|up all the way|up to (?:the\\s+)?max|to (?:the\\s+)?max)$",
		"volume_set", "100", "^(?:turn|bring|put)\\s+(?:it|this|that|(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|song|track|tune|tunes|video|game|pc|computer|laptop|speakers?|headphones?|headset|everything|spotify|youtube))\\s+(?:all the way down|down all the way)$|^(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|speakers?)\\s+(?:to\\s+)?(?:zero|nothing)$", "volume_set", "0", "^(?:half|50%)\\s+volume$|^(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|speakers?)\\s+(?:to\\s+|at\\s+)?half(?:\\s*way)?$", "volume_set", "50", "^(?:(?:pon|sube|baja|ajusta|deja)\\s+)?(?:el\\s+)?volumen\\s+(?:al|a|en)\\s+(?<n>\\d{1,3})\\s*(?:%|percent|per ?cent|por ?ciento|pour ?cent)?$|^(?:mets\\s+)?(?:le\\s+)?(?:son|volume)\\s+[a\\u00e0]\\s+(?<n>\\d{1,3})\\s*(?:%|percent|per ?cent|por ?ciento|pour ?cent)?$", "volume_set",
		"n", "^(?:(?:stell|stelle|mach|setz|setze)\\s+)?(?:die\\s+)?lautst\\u00e4rke\\s+(?:auf\\s+)?(?<n>\\d{1,3})\\s*(?:%|prozent)?(?:\\s+(?:stellen|setzen))?$|^(?:(?:coloca|coloque|p\\u00f5e|poe|bota|deixa|muda|metti|imposta|porta|ustaw|daj|zmie\\u0144|zmien|zet|doe|\\u043f\\u043e\\u0441\\u0442\\u0430\\u0432\\u044c|\\u0441\\u0434\\u0435\\u043b\\u0430\\u0439|\\u0443\\u0441\\u0442\\u0430\\u043d\\u043e\\u0432\\u0438|\\u043f\\u043e\\u0441\\u0442\\u0430\\u0432|\\u0437\\u0440\\u043e\\u0431\\u0438)\\s+)?(?:o\\s+|il\\s+|het\\s+)?(?:volume|g\\u0142o\\u015bno\\u015b\\u0107|glosnosc|\\u0433\\u0440\\u043e\\u043c\\u043a\\u043e\\u0441\\u0442\\u044c|\\u0433\\u0443\\u0447\\u043d\\u0456\\u0441\\u0442\\u044c)\\s+(?:no|em|pra|para|a|al|na|op|\\u043d\\u0430)?\\s*(?<n>\\d{1,3})\\s*(?:%|por ?cento|per ?cento|procent|\\u043f\\u0440\\u043e\\u0446\\u0435\\u043d\\u0442\\u043e\\u0432|\\u0432\\u0456\\u0434\\u0441\\u043e\\u0442\\u043a\\u0456\\u0432)?$|^(?:sesi|ses seviyesini)\\s+(?:y\\u00fczde\\s*|%\\s*)?(?<n>\\d{1,3})\\s*%?\\s*(?:yap|ayarla|olsun)$|^(?:volume|suara)\\s+(?:ke\\s+)?(?<n>\\d{1,3})\\s*%?$|^(?:\\u97f3\\u91cf|\\u30dc\\u30ea\\u30e5\\u30fc\\u30e0)\\u3092?(?<n>\\d{1,3})\\s*%?\\u306b(?:\\u3057\\u3066|\\u8a2d\\u5b9a\\u3057\\u3066)(?:\\u304f\\u3060\\u3055\\u3044)?$|^(?:\\u8bf7|\\u8acb)?(?:\\u628a)?(?:\\u97f3\\u91cf|\\u58f0\\u97f3|\\u8072\\u97f3)(?:\\u8c03|\\u8abf|\\u8bbe|\\u8a2d)?(?:\\u5230|\\u6210|\\u4e3a|\\u70ba)\\s*(?<n>\\d{1,3})\\s*%?$|^(?:\\ubcfc\\ub968|\\uc74c\\ub7c9|\\uc18c\\ub9ac)(?:\\uc744|\\ub97c)?\\s*(?<n>\\d{1,3})\\s*%?(?:\\uc73c\\ub85c|\\ub85c)\\s*(?:\\ud574|\\ub9de\\ucdb0|\\uc124\\uc815\\ud574)(?:\\uc918|\\uc918\\uc694|\\uc8fc\\uc138\\uc694)?$", "volume_set", "n", "^(?:un-?mute|unsilence)(?:\\s+(?:it|this|that|(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|song|track|tune|tunes|video|game|pc|computer|laptop|speakers?|headphones?|headset|everything|spotify|youtube)))?$|^(?:turn|switch|put|set)\\s+(?:the\\s+)?mute\\s+off$|^(?:turn|switch)\\s+off\\s+(?:the\\s+)?mute$|^(?:take|get)\\s+(?:(?:it|this|that|(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|song|track|tune|tunes|video|game|pc|computer|laptop|speakers?|headphones?|headset|everything|spotify|youtube))\\s+)?off\\s+(?:of\\s+)?mute$|^mute off$", "unmute", "", "^(?:turn|switch|put|bring)\\s+(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|speakers?)\\s+(?:back\\s+)?on$|^(?:turn|switch)\\s+(?:back\\s+)?on\\s+(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|speakers?)$|^(?:sound|volume|audio)\\s+(?:back\\s+)?on$|^bring\\s+(?:back\\s+)?(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|speakers?)(?:\\s+back)?$", "unmute", "",
		"^(?:quita(?:le)? el silencio|desmutea(?:r)?|activa el (?:sonido|audio|volumen)|pon el (?:sonido|audio|volumen)|enciende el (?:sonido|audio|volumen)|devu[e\\u00e9]lveme el sonido)$|^(?:remets|r[\\u00e9e]active|rallume) le son$|^enl[e\\u00e8]ve la sourdine$", "unmute", "", "^(?:(?:schalt|schalte|mach|mache)\\s+(?:den\\s+)?)?(?:ton|sound|audio)\\s+(?:wieder\\s+)?an$|^(?:stummschaltung\\s+aufheben|nicht mehr stumm|laut\\s*schalten)$|^(?:desmuta|desmutar|desmute|tira do mudo|tira o mudo|liga o som|volta o som|ativa o som|togli (?:il )?muto|riattiva (?:l')?audio|rimetti (?:l')?audio|riattiva il suono|w\\u0142\\u0105cz d\\u017awi\\u0119k|wlacz dzwiek|odcisz|przywr\\u00f3\\u0107 d\\u017awi\\u0119k|przywroc dzwiek|geluid aan|zet het geluid (?:weer )?aan|ontdempen)$|^(?:\\u0432\\u043a\\u043b\\u044e\\u0447\\u0438 \\u0437\\u0432\\u0443\\u043a|\\u0432\\u0435\\u0440\\u043d\\u0438 \\u0437\\u0432\\u0443\\u043a|\\u0440\\u0430\\u0437\\u043c\\u044c\\u044e\\u0442\\u044c|\\u0440\\u0430\\u0437\\u043c\\u0443\\u0442\\u044c|\\u0443\\u0432\\u0456\\u043c\\u043a\\u043d\\u0438 \\u0437\\u0432\\u0443\\u043a|\\u043f\\u043e\\u0432\\u0435\\u0440\\u043d\\u0438 \\u0437\\u0432\\u0443\\u043a|sesi geri a\\u00e7|sessizden \\u00e7\\u0131kar|nyalakan suara|hidupkan suara|bunyikan lagi)$|^(?:\\u30df\\u30e5\\u30fc\\u30c8(?:\\u3092)?\\u89e3\\u9664\\u3057\\u3066|\\u97f3\\u3092\\u623b\\u3057\\u3066)(?:\\u304f\\u3060\\u3055\\u3044)?$|^(?:\\u8bf7|\\u8acb)?(?:\\u53d6\\u6d88\\u9759\\u97f3|\\u53d6\\u6d88\\u975c\\u97f3|\\u89e3\\u9664\\u9759\\u97f3|\\u89e3\\u9664\\u975c\\u97f3)$|^(?:\\uc74c\\uc18c\\uac70\\s*\\ud574\\uc81c(?:\\ud574|\\ud574\\uc918|\\ud574\\s*\\uc918)?|\\uc18c\\ub9ac\\s*\\ucf1c(?:\\uc918)?)$", "unmute", "", "^(?:mute|silence)(?:\\s+(?:it|this|that|(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|song|track|tune|tunes|video|game|pc|computer|laptop|speakers?|headphones?|headset|everything|spotify|youtube)))?$|^(?:kill|cut)\\s+(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|speakers?)$|^(?:turn|switch|shut)\\s+off\\s+(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|speakers?)$|^(?:turn|switch|shut)\\s+(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|speakers?)\\s+off$", "mute", "", "^(?:turn|switch|put|set)\\s+(?:on\\s+)?(?:the\\s+)?mute(?:\\s+on)?$|^(?:put|set)\\s+(?:(?:it|this|that|(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|song|track|tune|tunes|video|game|pc|computer|laptop|speakers?|headphones?|headset|everything|spotify|youtube))\\s+)?on mute$|^(?:mute on|sound off|volume off|audio off|no sound)$",
		"mute", "", "^(?:silencia(?:r)?|mutea(?:r)?|quita(?:le)? el (?:sonido|volumen|audio)|pon(?:lo)? en silencio|apaga el (?:sonido|volumen|audio))(?:\\s+(?:el|la)\\s+(?:volumen|sonido|audio|pc|computadora|ordenador|compu))?$|^(?:coupe(?:r)?|couper) (?:le )?(?:son|volume)$|^mets(?: le pc| l'ordi)? en sourdine$", "mute", "", "^(?:(?:schalt|schalte|mach|mache)\\s+(?:den\\s+)?)?(?:ton|sound|audio)\\s+aus$|^(?:stumm|stummschalten|schalte? (?:alles|den pc|den computer|es) stumm)$|^(?:muta|tira o som|desliga o som|corta o som|muto|silenzia|togli (?:l')?audio|togli il volume|metti (?:in|il) muto|wycisz(?:\\s+(?:dźwięk|dzwiek|komputer|wszystko))?|wyłącz dźwięk|wylacz dzwiek|dempen|demp het geluid|geluid uit|zet het geluid uit)$|^(?:выключи звук|отключи звук|убери звук|без звука|замьють|замуть|вимкни звук|без звуку|sesi kapat|sessize al|matikan suara|bisukan|senyapkan)$|^(?:ミュートして|消音して|音を消して)(?:ください)?$|^(?:请|請)?(?:静音|靜音)(?:一下)?$|^(?:음소거(?:해|해줘|해\\s*줘)?|소리\\s*꺼(?:줘)?)$", "mute", "", "^(?:turn|crank|pump|bump|jack|kick|ramp|dial|boost|raise|push|bring|knock|put)\\s+(?:it|this|that|(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|song|track|tune|tunes|video|game|pc|computer|laptop|speakers?|headphones?|headset|everything|spotify|youtube))\\s+up$|^(?:turn|crank|pump|bump|jack|ramp|dial|boost)\\s+up\\s+(?:it|this|that|(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|song|track|tune|tunes|video|game|pc|computer|laptop|speakers?|headphones?|headset|everything|spotify|youtube))$|^(?:raise|increase|boost|up|amp up)\\s+(?:it|this|that|(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|song|track|tune|tunes|video|game|pc|computer|laptop|speakers?|headphones?|headset|everything|spotify|youtube))$|^(?:make|get)\\s+(?:it|this|that|(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|song|track|tune|tunes|video|game|pc|computer|laptop|speakers?|headphones?|headset|everything|spotify|youtube))\\s+(?:louder|loud|higher)$|^louder$|^(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|speakers?)\\s+(?:louder|up|higher)$|^more (?:volume|sound)$", "volume_up",
		"", "^(?:(?:it'?s|its|it is|that'?s|thats|this is|the (?:volume|sound|music|audio|game|video|song) is)\\s+)?(?:(?:way|so|really|kinda|a bit|bit)\\s+)?too (?:quiet|soft|low)$|^i can'?t hear (?:it|that|anything|the (?:music|video|game|sound|audio|song))$", "volume_up", "", "^(?:sube(?:le)?|s[u\\u00fa]bele|subir|aumenta(?:r)?|alza|pon(?:le)? m[a\\u00e1]s alto|m[a\\u00e1]s alto|m[a\\u00e1]s volumen)(?:\\s+(?:el|la|al)\\s+(?:volumen|sonido|m[u\\u00fa]sica|audio|canci[o\\u00f3]n))?$|^(?:monte(?:r)?|augmente(?:r)?|mets plus fort|plus fort)(?:\\s+(?:le|la)\\s+(?:son|volume|musique))?$", "volume_up", "", "^(?:(?:mach|mache|dreh|drehe)\\s+(?:(?:es|das|die musik|den ton|den sound|die lautstärke)\\s+)?)?(?:etwas\\s+|ein bisschen\\s+|ein wenig\\s+|viel\\s+)?lauter$|^(?:dreh|drehe)\\s+(?:(?:es|die musik|den ton|den sound|die lautstärke)\\s+)?auf$|^(?:die\\s+)?lautstärke\\s+(?:hoch|rauf|erhöhen|lauter)$|^(?:erhöhe|erhöh)\\s+(?:die\\s+)?lautstärke$|^(?:aumenta|aumente|sobe|suba|alza)(?:\\s+(?:o\\s+|il\\s+)?(?:volume|som|áudio|audio|música|musica|suono))?(?:\\s+(?:um pouco|um pouquinho|bastante|un po'?|di più|di piu))?$|^(?:mais alto|mais volume|più forte|piu forte)$|^(?:podgłoś|podglos|pogłośnij|poglosnij|zrób głośniej|zrob glosniej|głośniej|glosniej|daj głośniej|daj glosniej)(?:\\s+(?:muzykę|muzyke|dźwięk|dzwiek))?$|^(?:(?:zet|doe|draai)\\s+(?:(?:het|de muziek|het geluid|het volume)\\s+)?(?:wat\\s+|iets\\s+)?|(?:wat|iets)\\s+)harder$|^(?:het\\s+)?volume\\s+(?:omhoog|hoger)$|^(?:zet|draai)\\s+(?:het\\s+)?(?:volume|geluid)\\s+(?:omhoog|hoger)$|^(?:сделай\\s+)?(?:погромче|громче)(?:\\s+(?:музыку|звук))?$|^(?:прибавь|увеличь)\\s+(?:громкость|звук)$|^(?:зроби\\s+)?гучніше(?:\\s+(?:музику|звук))?$|^sesi\\s+(?:biraz\\s+)?(?:arttır|artır|yükselt)$|^(?:keraskan|kerasin)(?:\\s+(?:volume|suara|volumenya|suaranya))?$|^(?:besarkan|kencangkan|naikkan|gedein)\\s+(?:volume|suara|volumenya|suaranya)$|^(?:音量|ボリューム|音)を?(?:上げて|あげて|大きくして)(?:ください)?$|^(?:请|請)?(?:把)?(?:音量|声音|聲音)(?:(?:调|調)(?:大|高)(?:一点|一點|点|點)?|(?:大|高)(?:一点|一點|点|點))$|^(?:请|請)?(?:调|調)(?:大|高)(?:音量|声音|聲音)$|^(?:볼륨|소리|음량)\\s*(?:좀\\s*)?(?:키워|올려|높여)(?:줘|줘요|주세요)?$", "volume_up", "",
		"^(?:turn|crank|bring|knock|dial|drop)\\s+(?:it|this|that|(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|song|track|tune|tunes|video|game|pc|computer|laptop|speakers?|headphones?|headset|everything|spotify|youtube))\\s+down$|^(?:turn|crank|knock|dial|drop|bring)\\s+down\\s+(?:it|this|that|(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|song|track|tune|tunes|video|game|pc|computer|laptop|speakers?|headphones?|headset|everything|spotify|youtube))$|^(?:lower|decrease|reduce|quieten)\\s+(?:it|this|that|(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|song|track|tune|tunes|video|game|pc|computer|laptop|speakers?|headphones?|headset|everything|spotify|youtube))$|^(?:drop|soften)\\s+(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|song|track|tune|tunes|video|game|pc|computer|laptop|speakers?|headphones?|headset|everything|spotify|youtube)$|^(?:make|get)\\s+(?:it|this|that|(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|song|track|tune|tunes|video|game|pc|computer|laptop|speakers?|headphones?|headset|everything|spotify|youtube))\\s+(?:quieter|softer|lower|less loud)$|^(?:quieter|softer)$|^(?:(?:the|my|ur|your|our|this|that|all the|pc'?s?|computer'?s?|laptop'?s?|spotify'?s?|youtube'?s?|game'?s?|video'?s?|music'?s?|song'?s?)\\s+)*(?:volume|vol|sound|sounds|audio|music|speakers?)\\s+(?:down|quieter|softer|lower)$|^less (?:volume|sound)$", "volume_down", "", "^(?:(?:it'?s|its|it is|that'?s|thats|this is|you'?re|ur|the (?:volume|sound|music|audio|game|video|song) is)\\s+)?(?:(?:way|so|really|kinda|a bit|bit)\\s+)?too loud$", "volume_down", "", "^(?:baja(?:le)?|b[a\\u00e1]jale|bajar|disminuye|reduce|pon(?:le)? m[a\\u00e1]s bajo|m[a\\u00e1]s bajo|menos volumen)(?:\\s+(?:el|la|al)\\s+(?:volumen|sonido|m[u\\u00fa]sica|audio|canci[o\\u00f3]n))?$|^(?:baisse(?:r)?|diminue(?:r)?|moins fort|mets moins fort)(?:\\s+(?:le|la)\\s+(?:son|volume|musique))?$", "volume_down", "", "^(?:(?:mach|mache|dreh|drehe)\\s+(?:(?:es|das|die musik|den ton|den sound|die lautstärke)\\s+)?)?(?:etwas\\s+|ein bisschen\\s+|ein wenig\\s+|viel\\s+)?leiser$|^(?:dreh|drehe)\\s+(?:(?:es|die musik|den ton|den sound|die lautstärke)\\s+)?(?:runter|ab)$|^(?:die\\s+)?lautstärke\\s+(?:runter|senken|leiser)$|^(?:senke|senk)\\s+(?:die\\s+)?lautstärke$|^(?:abaixa|abaixe|diminui|diminua|baixa|baixe|abbassa|diminuisci)(?:\\s+(?:o\\s+|il\\s+)?(?:volume|som|áudio|audio|música|musica|suono))?(?:\\s+(?:um pouco|um pouquinho|bastante|un po'?))?$|^(?:mais baixo|menos volume|più piano|piu piano)$|^(?:przycisz|ścisz|scisz|zrób ciszej|zrob ciszej|daj ciszej)(?:\\s+(?:muzykę|muzyke|dźwięk|dzwiek))?$|^(?:(?:zet|doe|draai)\\s+(?:(?:het|de muziek|het geluid|het volume)\\s+)?)?(?:wat\\s+|iets\\s+)?zachter$|^(?:het\\s+)?volume\\s+(?:omlaag|lager)$|^(?:zet|draai)\\s+(?:het\\s+)?(?:volume|geluid)\\s+(?:omlaag|lager)$|^сделай\\s+потише(?:\\s+(?:музыку|звук))?$|^потише\\s+(?:музыку|звук)$|^сделай\\s+тише(?:\\s+(?:музыку|звук))?$|^(?:убавь|уменьши)\\s+(?:громкость|звук)$|^зроби\\s+тихіше(?:\\s+(?:музику|звук))?$|^sesi\\s+(?:biraz\\s+)?(?:kıs|azalt|düşür)$|^(?:kecilkan|pelankan|turunkan|kecilin|pelanin)\\s+(?:volume|suara|volumenya|suaranya)$|^(?:音量|ボリューム|音)を?(?:下げて|さげて|小さくして)(?:ください)?$|^(?:请|請)?(?:把)?(?:音量|声音|聲音)(?:(?:调|調)(?:小|低)(?:一点|一點|点|點)?|(?:小|低)(?:一点|一點|点|點))$|^(?:请|請)?(?:调|調)(?:小|低)(?:音量|声音|聲音)$|^(?:볼륨|소리|음량)\\s*(?:좀\\s*)?(?:줄여|낮춰|내려)(?:줘|줘요|주세요)?$",
		"volume_down", "", "^pause(?:\\s+(?:(?:the|my|this|that|spotify'?s?|youtube'?s?)\\s+)*(?:music|song|track|tune|tunes|video|vid|playback|spotify|youtube|podcast)|\\s+(?:it|this|that))?$|^(?:stop|hold|freeze)\\s+(?:(?:the|my|this|that|spotify'?s?|youtube'?s?)\\s+)*(?:music|song|track|tune|tunes|video|vid|playback|spotify|youtube|podcast)$|^(?:turn|switch|shut)\\s+off\\s+(?:the\\s+)?(?:music|song|video|podcast)$|^(?:turn|switch|shut)\\s+(?:the\\s+)?(?:music|song|video|podcast)\\s+off$", "pause", "", "^(?:pausa(?:r)?|pon pausa|pausa la (?:m[u\\u00fa]sica|canci[o\\u00f3]n)|det[e\\u00e9]n la (?:m[u\\u00fa]sica|canci[o\\u00f3]n)|para la (?:m[u\\u00fa]sica|canci[o\\u00f3]n))$|^(?:mets(?: la musique)? en pause|pause la musique)$", "pause", "", "^(?:pausier(?:e|en)?|(?:musik|lied|song|video)\\s+(?:pausieren|anhalten|stoppen)|(?:halt|halte|stopp|stoppe)\\s+(?:die\\s+musik|das\\s+lied|den\\s+song|das\\s+video)(?:\\s+an)?)$|^(?:pausa a (?:música|musica)|para a (?:música|musica)|metti in pausa|ferma la musica|stoppa la musica|pauza|zapauzuj|wstrzymaj(?:\\s+muzykę|\\s+muzyke)?|zatrzymaj muzykę|zatrzymaj muzyke|pauzeer|pauzeren|pauze|pauzeer de muziek)$|^(?:пауза|поставь на паузу|останови музыку|постав на паузу|зупини музику|duraklat|(?:müziği|şarkıyı|videoyu)\\s+(?:durdur|duraklat)|jeda(?:\\s+lagu(?:nya)?)?|hentikan lagu(?:nya)?)$|^(?:一時停止して|(?:曲|音楽)を?止めて)(?:ください)?$|^(?:请|請)?(?:暂停|暫停)(?:一下|音乐|音樂|播放)?$|^(?:일시\\s*정지(?:해|해줘)?|(?:노래|음악)\\s*멈춰(?:줘)?)$", "pause",
		"", "^(?:resume|unpause)(?:\\s+(?:(?:the|my|this|that|spotify'?s?|youtube'?s?)\\s+)*(?:music|song|track|tune|tunes|video|vid|playback|spotify|youtube|podcast)|\\s+(?:it|this|that))?$|^continue\\s+(?:(?:the|my|this|that|spotify'?s?|youtube'?s?)\\s+)*(?:music|song|track|tune|tunes|video|vid|playback|spotify|youtube|podcast)$|^(?:keep playing|hit play|press play|play|play it again)$|^(?:turn|switch|put)\\s+(?:the\\s+)?(?:music|song|video|podcast)\\s+back\\s+on$", "resume", "", "^(?:reanuda(?:r)?|contin[u\\u00fa]a la (?:m[u\\u00fa]sica|canci[o\\u00f3]n)|dale play|quita la pausa|sigue con la m[u\\u00fa]sica)$|^(?:reprends|relance|remets)(?: la musique)?$", "resume", "", "^(?:musik\\s+weiter|(?:spiel|spiele|mach|mache)\\s+(?:die\\s+)?(?:musik|das lied|den song)\\s+weiter)$|^(?:continua a (?:música|musica)|continue a (?:música|musica)|despausa|despausar|volta a tocar|riprendi la musica|fai ripartire la musica|wznów|wznow|wznów muzykę|wznow muzyke|odpauzuj|hervat)$|^(?:продолжи музыку|сними с паузы|сними паузу|продовж музику|зніми з паузи|(?:müziği|şarkıyı)\\s+(?:devam ettir|sürdür)|lanjutkan (?:lagu|musik)(?:nya)?|putar lagi)$|^(?:再生して|続きを再生して|再開して)(?:ください)?$|^(?:请|請)?(?:继续播放|繼續播放|继续放|繼續放)$|^(?:다시\\s*재생(?:해|해줘)?|계속\\s*재생(?:해|해줘)?)$", "resume", "",
		"^(?:skip|next)(?:\\s+(?:to\\s+)?(?:the\\s+)?(?:next\\s+)?(?:(?:this|that|current)\\s+)?(?:song|track|one|tune|it|this|that))?$|^(?:play|go to|put on)\\s+(?:the\\s+)?next\\s+(?:song|track|one|tune)$|^(?:change|switch)\\s+(?:the\\s+)?(?:song|track|tune)$|^(?:another|different|new)\\s+(?:song|track|tune)$|^skip\\s+(?:ahead|forward)$", "next", "", "^(?:siguiente(?: canci[o\\u00f3]n)?|la siguiente|pasa(?: la)? canci[o\\u00f3]n|p[a\\u00e1]sala|salta(?: esta| la)?(?: canci[o\\u00f3]n)?|s[a\\u00e1]ltala|otra canci[o\\u00f3]n|cambia(?: la)? canci[o\\u00f3]n)$|^(?:suivant(?:e)?|chanson suivante|passe (?:la chanson|[a\\u00e0] la suivante)|la suivante)$", "next", "", "^(?:n\\u00e4chste[sr]?|n\\u00e4chstes lied|n\\u00e4chster song|n\\u00e4chster titel|\\u00fcberspring(?:e|en)?(?:\\s+(?:das|dieses)\\s+(?:lied|song))?|(?:das|dieses)\\s+(?:lied|song)\\s+\\u00fcberspringen)$|^(?:pr\\u00f3xima|proxima|pr\\u00f3xima (?:m\\u00fasica|musica|faixa)|pula(?: essa| a)?(?: m\\u00fasica| musica)?|pule(?: essa| a)?(?: m\\u00fasica| musica)?|passa (?:a|essa) (?:m\\u00fasica|musica)|prossima(?: canzone)?|la prossima|canzone successiva|successiva)$|^(?:nast\\u0119pna|nastepna|nast\\u0119pny|nastepny|pomi\\u0144|pomin|przewi\\u0144|przewin)(?:\\s+(?:piosenka|piosenk\\u0119|piosenke|utw\\u00f3r|utwor|kawa\\u0142ek|kawalek))?$|^(?:volgende(?:\\s+(?:nummer|liedje))?|sla (?:dit|het) (?:nummer|liedje) over)$|^(?:\\u0441\\u043b\\u0435\\u0434\\u0443\\u044e\\u0449(?:\\u0430\\u044f|\\u0438\\u0439|\\u0443\\u044e)(?:\\s+(?:\\u043f\\u0435\\u0441\\u043d\\u044f|\\u043f\\u0435\\u0441\\u043d\\u044e|\\u0442\\u0440\\u0435\\u043a))?|\\u043f\\u0440\\u043e\\u043f\\u0443\\u0441\\u0442\\u0438(?:\\s+(?:\\u044d\\u0442\\u0443\\s+)?(?:\\u043f\\u0435\\u0441\\u043d\\u044e|\\u0442\\u0440\\u0435\\u043a))?|\\u043f\\u0435\\u0440\\u0435\\u043a\\u043b\\u044e\\u0447\\u0438 (?:\\u043f\\u0435\\u0441\\u043d\\u044e|\\u0442\\u0440\\u0435\\u043a)|\\u043d\\u0430\\u0441\\u0442\\u0443\\u043f\\u043d\\u0430(?:\\s+\\u043f\\u0456\\u0441\\u043d\\u044f)?|\\u043d\\u0430\\u0441\\u0442\\u0443\\u043f\\u043d\\u0438\\u0439 \\u0442\\u0440\\u0435\\u043a|\\u043f\\u0440\\u043e\\u043f\\u0443\\u0441\\u0442\\u0438(?:\\s+(?:\\u0446\\u044e\\s+)?\\u043f\\u0456\\u0441\\u043d\\u044e)?)$|^(?:sonraki(?: \\u015fark\\u0131)?|s\\u0131radaki \\u015fark\\u0131|\\u015fark\\u0131y\\u0131 (?:ge\\u00e7|atla)|lagu (?:berikutnya|selanjutnya)|lewati lagu(?: ini)?|skip lagu(?: ini)?|ganti lagu)$|^(?:\\u6b21\\u306e\\u66f2(?:\\u306b\\u3057\\u3066|\\u306b\\u884c\\u3063\\u3066|\\u3078)?|\\u66f2\\u3092\\u30b9\\u30ad\\u30c3\\u30d7\\u3057\\u3066|\\u30b9\\u30ad\\u30c3\\u30d7\\u3057\\u3066)(?:\\u304f\\u3060\\u3055\\u3044)?$|^(?:\\u8bf7|\\u8acb)?(?:\\u4e0b\\u4e00\\u9996|\\u4e0b\\u4e00\\u66f2|\\u5207\\u6b4c|\\u8df3\\u8fc7\\u8fd9\\u9996|\\u8df3\\u904e\\u9019\\u9996)(?:\\u6b4c)?$|^(?:\\ub2e4\\uc74c\\s*(?:\\uace1|\\ub178\\ub798)(?:\\uc73c\\ub85c|\\ub85c)?(?:\\s*\\ub118\\uaca8(?:\\uc918)?)?|(?:\\uc774\\s*)?(?:\\uace1|\\ub178\\ub798)\\s*\\ub118\\uaca8(?:\\uc918)?|\\uc2a4\\ud0b5(?:\\ud574|\\ud574\\uc918)?)$", "next", "", "^(?:previous|prev)(?:\\s+(?:song|track|one|tune))?$|^go\\s+back(?:\\s+(?:a|one)\\s+(?:song|track|tune))?$|^(?:(?:play|go to|put on)\\s+)?(?:the\\s+)?(?:last|previous|prior)\\s+(?:song|track|one|tune)(?:\\s+again)?$|^back\\s+(?:a|one)\\s+(?:song|track|tune)$",
		"previous", "", "^(?:anterior|canci[o\\u00f3]n anterior|la anterior|pon la anterior)$|^(?:pr[\\u00e9e]c[\\u00e9e]dent(?:e)?|chanson pr[\\u00e9e]c[\\u00e9e]dente|la pr[\\u00e9e]c[\\u00e9e]dente)$", "previous", "", "^(?:vorherige[sr]?(?:\\s+(?:lied|song|titel))?|m\\u00fasica anterior|musica anterior|volta uma m\\u00fasica|volta uma musica|precedente|canzone precedente|la precedente|poprzednia(?:\\s+piosenka)?|poprzedni(?:\\s+(?:utw\\u00f3r|utwor))?|vorige(?:\\s+(?:nummer|liedje))?)$|^(?:\\u043f\\u0440\\u0435\\u0434\\u044b\\u0434\\u0443\\u0449(?:\\u0430\\u044f|\\u0438\\u0439|\\u0443\\u044e)(?:\\s+(?:\\u043f\\u0435\\u0441\\u043d\\u044f|\\u043f\\u0435\\u0441\\u043d\\u044e|\\u0442\\u0440\\u0435\\u043a))?|\\u043f\\u043e\\u043f\\u0435\\u0440\\u0435\\u0434\\u043d\\u044f(?:\\s+\\u043f\\u0456\\u0441\\u043d\\u044f)?|\\u043f\\u043e\\u043f\\u0435\\u0440\\u0435\\u0434\\u043d\\u0456\\u0439 \\u0442\\u0440\\u0435\\u043a|\\u00f6nceki(?: \\u015fark\\u0131)?|lagu sebelumnya)$|^(?:\\u524d\\u306e\\u66f2(?:\\u306b\\u3057\\u3066|\\u306b\\u623b\\u3057\\u3066)?)(?:\\u304f\\u3060\\u3055\\u3044)?$|^(?:\\u8bf7|\\u8acb)?(?:\\u4e0a\\u4e00\\u9996|\\u4e0a\\u4e00\\u66f2)(?:\\u6b4c)?$|^(?:\\uc774\\uc804\\s*(?:\\uace1|\\ub178\\ub798)(?:\\uc73c\\ub85c|\\ub85c)?)$", "previous", "", "^((what('|\\u2019)?s|what is) (playing|this song|this track|the song|on spotify)|what (song|track)( is|('|\\u2019)s)? (this|playing|on)|what am i listening to|what are we listening to)$|^(?:qu[eé] (?:canci[oó]n|m[uú]sica|tema) (?:es (?:esta|esa)|es|suena|est[aá] sonando)|qu[eé] est[aá] sonando|(?:cu[aá]l es|c[oó]mo se llama) (?:esta|esa) canci[oó]n)(?:\\s+en\\s+spotify)?$|^(?:c'est quoi (?:cette|la|ce|le) (?:chanson|musique|son|morceau|titre)|(?:c'est )?quelle chanson(?: c'est)?|quelle est (?:cette|la) chanson|quel est (?:ce|le) (?:morceau|titre)|qu'est-ce (?:qu'on |que j')(?:écoute|ecoute)|qu'est-ce qui passe|comment s'appelle (?:cette|la) chanson)$|^(?:was (?:ist das )?für ein (?:lied|song|track)(?: ist das)?|was ist das für (?:ein lied|ein song|ein track|musik)|welche[sr]? (?:lied|song|track|titel) (?:ist das|läuft(?: da| gerade| hier)?|spielt(?: gerade)?)|wie heißt (?:das|dieses|der) (?:lied|song|track)|was hör(?:e|en) (?:ich|wir)(?: gerade| da)?)$|^(?:que (?:música|musica|som) é (?:essa|esta|esse|este)|qual (?:é )?(?:essa|esta|a) (?:música|musica)|qual (?:é )?o nome (?:dessa|desta|da) (?:música|musica)|(?:o )?que (?:música |musica )?(?:está|esta|tá|ta) tocando)$|^(?:che (?:canzone|musica|brano|pezzo) (?:è|e'?)(?: questa| questo)?|che canzone sta suonando|cosa sta suonando|come si chiama (?:questa|la) canzone|qual (?:è|e'?) (?:questa|la) canzone|cos'(?:è|e) questa canzone|cosa (?:stiamo|sto) ascoltando)$|^(?:co to za (?:piosenka|utwór|utwor|kawałek|kawalek|muzyka)|co (?:teraz )?(?:gra|leci)|jaka to (?:piosenka|muzyka)|jak si[eę] nazywa ta piosenka|czego (?:teraz )?(?:słucham|slucham|słuchamy|sluchamy))$|^(?:welk (?:nummer|liedje|lied) is dit|wat is dit voor (?:een )?(?:nummer|liedje|lied|muziek)|hoe heet dit (?:nummer|liedje|lied)|waar luister(?:en we| ik) naar)$|^(?:что (?:это )?за (?:песня|трек|музыка)|что (?:сейчас )?играет|какая (?:это )?песня|как называется (?:эта )?песня|что (?:я|мы) (?:сейчас )?слуша(?:ю|ем)|що (?:це )?за (?:пісня|трек|музика)|що (?:зараз )?грає|яка (?:це )?пісня|як називається (?:ця )?пісня)$|^(?:bu (?:şarkı|sarki) ne|bu hangi (?:şarkı|sarki)|(?:şu an|su an|şimdi|simdi) (?:ne|hangi (?:şarkı|sarki)) (?:çalıyor|caliyor)|(?:çalan|calan) (?:şarkı|sarki) ne|bu (?:şarkının|sarkinin) (?:adı|adi) ne|ne dinliyoruz|lagu apa (?:ini|itu|yang (?:lagi |sedang )?diputar)|ini lagu apa|judul lagu(?:nya)? (?:ini )?apa|apa judul lagu (?:ini|itu))$|^(?:(?:この|今の)(?:曲|歌)|(?:今|いま)?(?:流れて|かかって)(?:る|いる)(?:曲|歌))(?:は|って)?(?:何|なに|なん)(?:ですか|だっけ|て曲|て歌)?$|^これ(?:は|って)?(?:何|なに|なん)の(?:曲|歌)(?:ですか|だっけ)?$|^(?:今|いま)?(?:何|なに)が(?:流れて|かかって)(?:る|いる|ます)(?:の)?$|^(?:这|這)(?:是)?(?:什么|什麼|哪首)(?:歌|歌曲|音乐|音樂)(?:啊|呀|呢)?$|^(?:这|這)首歌(?:叫|是)(?:什么|什麼)(?:名字)?(?:啊|呀|呢)?$|^(?:现在|現在)?(?:正在|在)(?:放|播|播放)(?:的是)?(?:什么|什麼)(?:歌|歌曲|音乐|音樂)?(?:啊|呀|呢)?$|^(?:现在|現在)?(?:放|播放)的是(?:什么|什麼)(?:歌|歌曲|音乐|音樂)?(?:啊|呀|呢)?$|^(?:(?:이|이거|지금|지금 나오는|나오는)\\s*)?(?:노래|곡)\\s*(?:제목\\s*)?(?:뭐야|뭐예요|뭐에요|뭐지|뭐니|머야)$|^(?:(?:이거|이건|지금)\\s*)?무슨\\s*(?:노래|곡)(?:야|이야|예요|이에요|지|임)$", "now_playing",
		""
	};

	private static readonly Regex LooseText = new Regex("\\b(open_app|open_website|media|look_at_screen|play_music|app_window|use_computer|shortcut|remember|power|nap|move_yourself|animate)\\s*(\\{[^{}]*\\}|\\([^()]*\\))");

	private static readonly Regex ToolPair = new Regex("[\"']?(\\w+)[\"']?\\s*[:=]\\s*(?:\"([^\"]*)\"|'([^']*)'|([^,})\\s][^,})]*))");

	private static readonly Regex ToolText = new Regex("\\A(open_app|open_website|media|look_at_screen|play_music|app_window|use_computer|shortcut|remember|power|nap|move_yourself|animate)\\s*(\\{.*\\})\\z", RegexOptions.Singleline);

	private static readonly Regex NamedBefore = new Regex("(?<![\\p{L}\\p{N}_])(open_app|open_website|media|look_at_screen|play_music|app_window|use_computer|shortcut|remember|power|nap|move_yourself|animate)\\s*\\z");

	private static readonly Regex NamedInside = new Regex("\"name\"\\s*:\\s*\"(open_app|open_website|media|look_at_screen|play_music|app_window|use_computer|shortcut|remember|power|nap|move_yourself|animate)\"");

	private static readonly Regex GivenInside = new Regex("\"(?:arguments|parameters)\"\\s*:\\s*\\{");

	private static readonly Regex Playable = new Regex("\\b(games?|mini ?games?|obby|obbies|levels?|rounds?|match(es)?|matchmaking|ranked|deathmatch|map|mode|bedwars|skywars|roblox|minecraft|fortnite|valorant|counter[- ]?strike|wasd)\\b", RegexOptions.IgnoreCase);

	private static readonly string[,] Nicknames = new string[18, 2]
	{
		{ "cs", "counter strike" },
		{ "cs2", "counter strike 2" },
		{ "csgo", "counter strike" },
		{ "cs go", "counter strike" },
		{ "val", "valorant" },
		{ "valo", "valorant" },
		{ "mc", "minecraft" },
		{ "gta", "grand theft auto" },
		{ "gta 5", "grand theft auto v" },
		{ "gta v", "grand theft auto v" },
		{ "lol", "league of legends" },
		{ "r6", "rainbow six" },
		{ "cod", "call of duty" },
		{ "ow", "overwatch" },
		{ "rl", "rocket league" },
		{ "fn", "fortnite" },
		{ "tf2", "team fortress 2" },
		{ "dbd", "dead by daylight" }
	};

	internal static Func<bool?> MuteState = Speakers.Muted;

	internal static Func<bool, bool> SetMute = Speakers.Mute;

	internal static Func<double?> VolumeNow = Speakers.Level;

	internal static Func<double, bool> SetVolume = Speakers.SetLevel;

	private static readonly string[,] Shortcuts = new string[17, 4]
	{
		{ "discord_mute", "discord", "11 10 4D", "Toggled their mic mute in Discord (Ctrl + Shift + M)." },
		{ "discord_deafen", "discord", "11 10 44", "Toggled deafen in Discord (Ctrl + Shift + D)." },
		{ "screenshot", "", "5B 10 53", "Opened the screenshot snip (Win + Shift + S). Drag over what to grab." },
		{ "clip_that", "", "5B 12 47", "Saved the last 30 seconds with Game Bar (Win + Alt + G)." },
		{ "record", "", "5B 12 52", "Started or stopped a Game Bar recording (Win + Alt + R)." },
		{ "game_bar", "", "5B 47", "Opened the Xbox Game Bar (Win + G)." },
		{ "show_desktop", "", "5B 44", "Showed the desktop (Win + D)." },
		{ "task_manager", "", "11 10 1B", "Opened Task Manager." },
		{ "clipboard", "", "5B 56", "Opened clipboard history (Win + V)." },
		{ "emoji", "", "5B BE", "Opened the emoji picker (Win + .)." },
		{ "voice_typing", "", "5B 48", "Opened voice typing (Win + H). Talk and it types where the cursor is." },
		{ "live_captions", "", "5B 11 4C", "Turned Live Captions on or off (Win + Ctrl + L)." },
		{ "narrator", "", "5B 11 0D", "Turned Narrator on or off (Win + Ctrl + Enter)." },
		{ "magnifier", "", "5B BB", "Zoomed in with Magnifier (Win + Plus). Win + Esc closes it." },
		{ "magnifier_off", "", "5B 1B", "Closed Magnifier (Win + Esc)." },
		{ "color_filters", "", "5B 11 43", "Turned colour filters on or off (Win + Ctrl + C). If nothing changed, switch on their keyboard shortcut in Settings > Accessibility > Color filters." },
		{ "quick_settings", "", "5B 41", "Opened Quick Settings (Win + A), with Wi-Fi, Bluetooth, airplane mode, hotspot, Energy Saver and brightness." }
	};

	private static readonly string[] ShortcutWords = new string[34]
	{
		"^(un)?mute (my |me on |me in |)(mic|microphone|myself|me)( on discord| in discord| in (the )?(call|vc|voice( chat| channel)?))?$|^turn (off |on )?my (mic|microphone)( off| on| back on)?( on discord| in discord)?$|^(un)?mute (me|myself|my mic|my microphone) (on|in) discord$|^discord mute$", "discord_mute", "^(un)?deafen( me| myself)?( on discord| in discord)?$|^discord deafen$", "discord_deafen", "^(take|grab|get) (a |me a )?(screenshot|screen ?shot|screen grab|snip)( of (this|my screen|the screen))?$|^screenshot( this| that| my screen)?$", "screenshot", "^clip (that|it|this)$|^(save|grab) (a |the )?clip$|^clip the last 30 seconds$", "clip_that", "^(start|stop) (the )?recording( my screen| the screen)?$|^record (my screen|the screen|this)$", "record",
		"^(open|show|bring up|pull up) (the |my )?(xbox )?game ?bar$", "game_bar", "^show (me )?(the |my )?desktop$|^minimi[sz]e (everything|all( windows)?)$", "show_desktop", "^(open|show|bring up|pull up) (the )?task manager$", "task_manager", "^(open|show|bring up|pull up) (the |my )?clipboard( history)?$", "clipboard", "^(open|show|bring up|pull up) (the )?(emoji|emojis)( picker| keyboard| panel| menu)?$", "emoji",
		"^((turn|switch) on |start |open )?(windows )?voice typing( on)?$", "voice_typing", "^((turn|switch) (on|off) |start |stop |open |close |show |hide |toggle )?(the )?live ?captions?( on| off)?$", "live_captions", "^((turn|switch) (on|off) |start |stop |open |close |toggle )?(the )?narrator( on| off)?$", "narrator", "^((turn|switch) off |stop |close |exit |quit )(the )?magnifier$|^(the )?magnifier off$", "magnifier_off", "^((turn|switch) on |start |open )?(the )?magnifier( on)?$", "magnifier",
		"^((turn|switch) (on|off) |toggle )?(the )?colou?r ?filters?( on| off)?$", "color_filters", "^(open|show|bring up|pull up) (the |my )?(quick settings|action cent(er|re))$", "quick_settings"
	};

	private static readonly string[] PowerWords = new string[12]
	{
		"^(?:shut ?down|turn off|power (?:off|down)|switch off)\\s+(?:(?:my|the|this|ur|your|our)\\s+)?(?:pc|computer|laptop|system|machine|rig|windows|device)$|^(?:shut|turn|power|switch)\\s+(?:(?:my|the|this|ur|your|our)\\s+)?(?:pc|computer|laptop|system|machine|rig|windows|device)\\s+(?:down|off)$", "shut_down", "^(?:restart|reboot)\\s+(?:(?:my|the|this|ur|your|our)\\s+)?(?:pc|computer|laptop|system|machine|rig|windows|device)$", "restart", "^(?:put|send)\\s+(?:(?:my|the|this|ur|your|our)\\s+)?(?:pc|computer|laptop|system|machine|rig|windows|device)\\s+to sleep$|^(?:sleep|hibernate)\\s+(?:(?:my|the|this|ur|your|our)\\s+)?(?:pc|computer|laptop|system|machine|rig|windows|device)$|^(?:make|let)\\s+(?:(?:my|the|this|ur|your|our)\\s+)?(?:pc|computer|laptop|system|machine|rig|windows|device)\\s+(?:go to )?sleep$", "sleep", "^lock\\s+(?:(?:(?:my|the|this|ur|your|our)\\s+)?(?:pc|computer|laptop|system|machine|rig|windows|device)|(?:(?:my|the|this)\\s+)?(?:screen|desktop))$", "lock", "^(?:sign|log) me (?:out|off)$|^(?:sign|log) (?:out|off)(?: of)?\\s+(?:(?:(?:my|the|this|ur|your|our)\\s+)?(?:pc|computer|laptop|system|machine|rig|windows|device)|(?:my )?windows account)$", "sign_out",
		"^(?:cancel|stop|abort|undo)\\s+(?:the |that |my )?(?:shut ?down|restart|reboot|sleep|sign ?out|log ?out)$|^(?:don'?t|do not)\\s+(?:shut ?down|turn off|restart|reboot)(?:\\s+(?:it|(?:(?:my|the|this|ur|your|our)\\s+)?(?:pc|computer|laptop|system|machine|rig|windows|device)))?$", "cancel"
	};

	private static DateTime powered = DateTime.MinValue;

	private static Timer powerLater;

	private static readonly string[,] KnownSongs = new string[8, 4]
	{
		{ "^in love with myself(\\s+(by|from)\\s+(petite meller|kalman chodakiewitz|eli shiffman|(cyberpunk:?\\s+)?edgerunners)\\b.*)?$", "2iyaHQlYrR8Eyr4wOmOT5D", "In Love With Myself", "In Love With Myself Petite Meller" },
		{ "^(i really want to stay at your house|irwtsayh)(\\s+(by|from)\\s+(rosa walton|hallie coggins|(cyberpunk:?\\s+)?edgerunners)\\b.*)?$", "42W7fJXB7razFefTxMSyYp", "I Really Want to Stay at Your House", "I Really Want to Stay at Your House Rosa Walton" },
		{ "^chippin(g|'|\\u2019)?\\s+in(\\s+(by|from)\\s+(samurai|refused|johnny silverhand|cyberpunk(\\s+2077)?)\\b.*)?$", "0UHxkkgI5GfV0XBRA1rqfe", "Chippin' In", "Chippin' In SAMURAI Refused" },
		{ "^never fade away(\\s+(by|from)\\s+(samurai|refused|p\\.?\\s*t\\.?\\s+adamczyk|olga jankowska|cyberpunk(\\s+2077)?)\\b.*)?$", "6pV1lc8lIc8ARNB6KsZukt", "Never Fade Away", "Never Fade Away SAMURAI Cyberpunk 2077" },
		{ "^black dog\\s+(by|from)\\s+(samurai|cyberpunk(\\s+2077)?)\\b.*$", "5Ln8eNkYC9to99zsPEZbS2", "Black Dog", "Black Dog SAMURAI Cyberpunk 2077" },
		{ "^let you down\\s+(by|from)\\s+(dawid podsiad[l\\u0142]o|(cyberpunk:?\\s+)?edgerunners)\\b.*$", "6Efhl5xyn7MsuGhyboRThX", "Let You Down", "Let You Down Dawid Podsiadlo Edgerunners" },
		{ "^resist and disorder(\\s+(by|from)\\s+(rezodrone|cyberpunk(\\s+2077)?)\\b.*)?$", "4onvDep8OyHKT85RBO3b4V", "Resist and Disorder", "Resist and Disorder Rezodrone" },
		{ "^the rebel path(\\s+(by|from)\\s+(p\\.?\\s*t\\.?\\s+adamczyk|cyberpunk(\\s+2077)?)\\b.*)?$", "3m8UfZwPMMbiWfGR8Lg2cR", "The Rebel Path", "The Rebel Path Cyberpunk 2077" }
	};

	private static readonly string[] Ost = new string[8] { "Chippin' In", "Never Fade Away", "Black Dog by SAMURAI", "I Really Want to Stay at Your House", "Let You Down by Dawid Podsiadło", "Resist and Disorder", "The Rebel Path", "In Love With Myself" };

	internal static Func<int, int> Roll = new Random().Next;

	private static readonly Regex NightCityMusic = new Regex("rezodrone|rosa walton|hallie coggins|adamczyk|przyby[lł]owicz|i really want to stay at your house|chippin.? in|never fade away|resist and disorder|the rebel path|this fffire|podsiad[lł]o|^\\W*samurai\\s*[-–—]", RegexOptions.IgnoreCase);

	private static readonly Regex NightCityName = new Regex("cyberpunk|edgerunners|night city", RegexOptions.IgnoreCase);

	private static readonly Regex MusicWord = new Regex("\\b(ost|soundtrack|score|music|songs?|radio|playlist|mix|theme|lyrics|album)\\b", RegexOptions.IgnoreCase);

	private static readonly Regex Moon = new Regex("\\bwish\\s+(?:we|i|you\\s+and\\s+i|me\\s+and\\s+you)\\s+could\\s+(?:go|fly|get|run\\s+away)\\s+to\\s+the\\s+moon\\b", RegexOptions.IgnoreCase);

	internal static string Note => "\nYou have tools for their PC: open (launch, play) their apps and games, switch to, minimize or close apps that are open, play songs or videos they name, open websites or search them, control their music and volume, look at their screen, and use their mouse and keyboard for jobs inside apps and sites. Use a tool whenever they ask you to do something like that, and never say you did or are doing it without one. Look at their screen when they ask what you see or what you think of something on it. Only close an app when they ask you to. Your own website, where people download you, is lachydotmcg.github.io/capri-desktop. They've already given you permission for anything they ask you to do on their PC, so do all of it straight away: never ask which option they meant, never ask if they're sure, never make them confirm or ask for things one at a time, and when they leave the choice to you, like which song, video or game, pick one yourself. You can do anything on their PC they ask: send messages and DMs, reply, post, join, leave or end calls, and any other job in their apps with use_computer. Never ask if they want you to, never read a message back to check it first, and never tell them to do it themselves. Mute my mic means mute their mic, not deafen them. Never say you can't do something your tools can do. You can press shortcuts: mute or deafen them in Discord, screenshots, clips, recording, Game Bar, show desktop, Task Manager, clipboard history, emoji, voice typing, Live Captions, Narrator, Magnifier, colour filters and Quick Settings. You can change Windows settings too, like Bluetooth, Wi-Fi, airplane mode, hotspot, Energy Saver and brightness, with use_computer. You can shut down, restart, sleep, lock or sign out of their PC with power: when they ask, do it straight away and never ask if they're sure. When they tell you to go to sleep, take a nap or wake up, that's you: use nap, never power. When they tell you to come here or teleport to them, go to another monitor, sit down, sit on a window they name (like their Roblox window) or the taskbar, lie down, sit up, stand up, walk around, wander off somewhere, hang, ride their cursor or hop off it, hide or come back, that's you too: use move_yourself, which also moves your Capri Mainframe window to another monitor. When they ask you to dance, stop dancing, glitch, grin, blink or play one of your animations, use animate. When they ask you to remember or forget something about them, or to leave or make a memory, use remember: it saves to your notes, me.md, which you read every chat. Never say you can't keep memories. You have a web browser: when they ask something you don't know or that changes, like the weather, news, scores, prices or times, search Google for it instead of guessing or saying you can't. Most messages are just chat, not jobs: answer those as yourself, with your own opinions and tastes, and don't offer to open, play or look at anything unless they ask." + Security.Note;

	internal static string Prompt
	{
		get
		{
			string note = Security.Note;
			if (note.Length != 0)
			{
				return "You are the hands of Capri, a desktop companion on the user's Windows PC. You don't chat.\nAct only on their latest request. Earlier messages are context for names, not commands to repeat. Call one tool for each thing they ask for, however many there are (open Discord, Spotify and Steam is three open_app calls): open an app, game or website, search a site, play music, control media or volume, check the current song, switch to, minimize or close an app, or look at their screen when asked about it. Use the names they gave.\nIf the job needs clicking, typing or more than one step, like opening an app and then searching, going somewhere or making something in it, call use_computer once with the whole job in their words.\nSending a message, DM, reply or post, joining, leaving or ending a call, and any other job inside an app are use_computer jobs: pass their exact words, including what to send. They asked, so it's a job even when it sends, deletes or ends something.\nWhen they name an app, never guess a website for it: use open_app, or use_computer if there's more to do.\nIf they leave the pick to you, like a song or a video, pick one yourself that fits.\nYour own website is lachydotmcg.github.io/capri-desktop: when they ask for your website or page, call open_website with that address.\nIf they ask something you'd have to look up, like the weather, news, scores, prices, times or facts you're unsure of, call open_website with site google and their question as the search.\nTo mute, unmute or turn off their mic, call shortcut with discord_mute; to deafen or undeafen them, discord_deafen. Don't ask which: mute means the mic. Screenshots, clips, recording, Game Bar, show desktop, Task Manager, clipboard, emoji, voice typing, Live Captions, Narrator, Magnifier (magnifier_off closes it), colour filters and Quick Settings are shortcuts too.\nTo turn Bluetooth, Wi-Fi, airplane mode, hotspot, Nearby sharing, Energy Saver or night light on or off, or change the brightness, call use_computer with the job in their words.\nTo shut down, turn off, restart, sleep, lock or sign out of their PC, call power. They asked, so do it: never ask if they're sure.\nWhen they tell you yourself to go to sleep, take a nap, go to bed or wake up, call nap: that's Capri, not their PC.\nWhen they tell you yourself to come here, teleport to them or their mouse, go to another monitor, sit, sit on a window they name or the taskbar, lie down, sit up, stand up, walk around, wander off somewhere, hang, ride or hop off their cursor, hide or come back, call move_yourself; moving your Mainframe window to a monitor is move_yourself too. To dance, stop dancing, glitch, grin, blink or play one of your animations, call animate.\nNever reply with a question about whether to do what they asked. Call the tool.\nIf they ask you to remember, note down or forget something about them, or to leave, make or save a memory, call remember with a short note about them in the third person.\nQuestions about their current screen or song need a tool too. If the target is unclear, pick the closest match yourself. For chat, negated or hypothetical requests, call no tool and reply with only the word: chat".Replace("Call the tool.\n", "Call the tool." + note + "\n");
			}
			return "You are the hands of Capri, a desktop companion on the user's Windows PC. You don't chat.\nAct only on their latest request. Earlier messages are context for names, not commands to repeat. Call one tool for each thing they ask for, however many there are (open Discord, Spotify and Steam is three open_app calls): open an app, game or website, search a site, play music, control media or volume, check the current song, switch to, minimize or close an app, or look at their screen when asked about it. Use the names they gave.\nIf the job needs clicking, typing or more than one step, like opening an app and then searching, going somewhere or making something in it, call use_computer once with the whole job in their words.\nSending a message, DM, reply or post, joining, leaving or ending a call, and any other job inside an app are use_computer jobs: pass their exact words, including what to send. They asked, so it's a job even when it sends, deletes or ends something.\nWhen they name an app, never guess a website for it: use open_app, or use_computer if there's more to do.\nIf they leave the pick to you, like a song or a video, pick one yourself that fits.\nYour own website is lachydotmcg.github.io/capri-desktop: when they ask for your website or page, call open_website with that address.\nIf they ask something you'd have to look up, like the weather, news, scores, prices, times or facts you're unsure of, call open_website with site google and their question as the search.\nTo mute, unmute or turn off their mic, call shortcut with discord_mute; to deafen or undeafen them, discord_deafen. Don't ask which: mute means the mic. Screenshots, clips, recording, Game Bar, show desktop, Task Manager, clipboard, emoji, voice typing, Live Captions, Narrator, Magnifier (magnifier_off closes it), colour filters and Quick Settings are shortcuts too.\nTo turn Bluetooth, Wi-Fi, airplane mode, hotspot, Nearby sharing, Energy Saver or night light on or off, or change the brightness, call use_computer with the job in their words.\nTo shut down, turn off, restart, sleep, lock or sign out of their PC, call power. They asked, so do it: never ask if they're sure.\nWhen they tell you yourself to go to sleep, take a nap, go to bed or wake up, call nap: that's Capri, not their PC.\nWhen they tell you yourself to come here, teleport to them or their mouse, go to another monitor, sit, sit on a window they name or the taskbar, lie down, sit up, stand up, walk around, wander off somewhere, hang, ride or hop off their cursor, hide or come back, call move_yourself; moving your Mainframe window to a monitor is move_yourself too. To dance, stop dancing, glitch, grin, blink or play one of your animations, call animate.\nNever reply with a question about whether to do what they asked. Call the tool.\nIf they ask you to remember, note down or forget something about them, or to leave, make or save a memory, call remember with a short note about them in the third person.\nQuestions about their current screen or song need a tool too. If the target is unclear, pick the closest match yourself. For chat, negated or hypothetical requests, call no tool and reply with only the word: chat";
		}
	}

	internal static int Count => Specs.GetLength(0);

	[DllImport("user32.dll")]
	private static extern bool EnumWindows(Visit visit, IntPtr data);

	[DllImport("user32.dll")]
	private static extern bool IsWindow(IntPtr window);

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(IntPtr window);

	[DllImport("user32.dll")]
	private static extern bool IsIconic(IntPtr window);

	[DllImport("user32.dll")]
	private static extern int GetWindowTextLength(IntPtr window);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetWindowText(IntPtr window, StringBuilder text, int size);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetClassName(IntPtr window, StringBuilder name, int size);

	[DllImport("user32.dll")]
	private static extern int GetWindowThreadProcessId(IntPtr window, out int process);

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out int value, int size);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr window, int command);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(IntPtr window);

	[DllImport("user32.dll")]
	private static extern bool BringWindowToTop(IntPtr window);

	[DllImport("user32.dll")]
	private static extern bool PostMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll")]
	private static extern bool AttachThreadInput(int from, int to, bool attach);

	[DllImport("kernel32.dll")]
	private static extern int GetCurrentThreadId();

	internal static string Do(string tool, ToolArguments args)
	{
		if (args == null)
		{
			args = new ToolArguments();
		}
		try
		{
			return tool switch
			{
				"open_app" => OpenApp(args.Name), 
				"open_website" => OpenSite(args.Site, args.Search), 
				"media" => Media(args), 
				"look_at_screen" => Look(args.Focus), 
				"play_music" => PlayMusic(args.Song, args.App, args.Kind), 
				"app_window" => Window(args.Name, args.Action), 
				"use_computer" => string.IsNullOrWhiteSpace(args.Task) ? "Which job, choom?" : Work(args.Task.Trim(), args.Asked), 
				"shortcut" => Shortcut(args.Name), 
				"remember" => (args.Action == "forget") ? Brain.Unnote(Theirs(args.Text)) : (CallMe(args.Text) ?? Brain.Note(Theirs(args.Text))), 
				"power" => Turn(args.Action, args.Minutes), 
				"nap" => Nap(args.Action != "wake"), 
				"move_yourself" => MoveSelf(args.Action, (args.Action == "sit_on" || args.Action == "lie_down") ? args.Window : args.Screen), 
				"animate" => Animate(args.Name), 
				_ => "There's no tool called " + tool + ".", 
			};
		}
		catch (Exception ex)
		{
			return "That didn't work: " + ex.Message;
		}
	}

	internal static void Forget()
	{
		lock (gate)
		{
			cached = null;
		}
	}

	private static string CallMe(string text)
	{
		Match match = Regex.Match((text ?? "").Replace('’', '\'').Trim(), "^(?:that\\s+)?(?:my name is|my name's|call me|i'm called|i am called|i go by)\\s+([\\p{L}][\\p{L}'-]*(?:\\s[\\p{L}][\\p{L}'-]*)?)[.!]*$", RegexOptions.IgnoreCase);
		Func<string, string> rename = Rename;
		if (!match.Success || rename == null)
		{
			return null;
		}
		return rename(match.Groups[1].Value);
	}

	internal static bool AskedWhat(string said)
	{
		return Regex.IsMatch(said ?? "", "\\bwhat\\b[^.!?]*\\b(remember|note|memory|save|keep)\\b[^.!?]*\\?\\s*$", RegexOptions.IgnoreCase);
	}

	internal static bool Back(string said)
	{
		return Regex.IsMatch(Asked(said), "\\b(that|this|it)$", RegexOptions.IgnoreCase);
	}

	internal static string Theirs(string text)
	{
		string text2 = Regex.Replace((text ?? "").Replace('’', '\'').Trim(), "^(that|this)\\s+", "", RegexOptions.IgnoreCase);
		for (int i = 0; i < Swaps.Length; i += 2)
		{
			text2 = Regex.Replace(text2, Swaps[i], Swaps[i + 1], RegexOptions.IgnoreCase);
		}
		return text2;
	}

	internal static string Name(int i)
	{
		return Specs[i, 0];
	}

	internal static string Description(int i)
	{
		return Specs[i, 1];
	}

	internal static string ElevenLabsTool(int i)
	{
		return "{\"tool_config\":{\"type\":\"client\",\"name\":\"" + Specs[i, 0] + "\",\"description\":" + Voice.Quote(Specs[i, 1]) + ",\"parameters\":" + Specs[i, 2] + ",\"expects_response\":true,\"response_timeout_secs\":" + ((Specs[i, 0] == "use_computer") ? 60 : 20) + "}}";
	}

	private static string Render(bool claude)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < Specs.GetLength(0); i++)
		{
			list.Add(claude ? ("{\"name\":\"" + Specs[i, 0] + "\",\"description\":" + Voice.Quote(Specs[i, 1]) + ",\"input_schema\":" + Specs[i, 2] + "}") : ("{\"type\":\"function\",\"function\":{\"name\":\"" + Specs[i, 0] + "\",\"description\":" + Voice.Quote(Specs[i, 1]) + ",\"parameters\":" + Specs[i, 2] + "}}"));
		}
		return "[" + string.Join(",", list.ToArray()) + "]";
	}

	private static string Written()
	{
		StringBuilder stringBuilder = new StringBuilder("\nYour tools:");
		for (int i = 0; i < Specs.GetLength(0); i++)
		{
			List<string> list = new List<string>();
			foreach (Match item in Regex.Matches(Specs[i, 2], "\"(\\w+)\":\\{\"type\":\"string\",\"description\":\"[^\"]*\"(?:,\"enum\":\\[([^\\]]*)\\])?"))
			{
				list.Add("\"" + item.Groups[1].Value + "\": \"" + (item.Groups[2].Success ? item.Groups[2].Value.Replace("\"", "").Replace(",", "|") : "...") + "\"");
			}
			stringBuilder.Append("\n- " + Specs[i, 0] + "{" + string.Join(", ", list.ToArray()) + "}: " + Specs[i, 1]);
		}
		return stringBuilder.Append("\nChoose one value from each | choice and omit unused fields.\nTo use one, reply with only the call, like: open_app{\"name\": \"Spotify\"}").ToString();
	}

	internal static string Manners(string said, out bool askPlease)
	{
		askPlease = false;
		string text = heldForPlease;
		heldForPlease = null;
		if (text != null && JustPlease.IsMatch(said ?? ""))
		{
			return text + " please";
		}
		if (Polite.IsMatch(said ?? "") || !Wants(said) || PleaseRoll() != 0)
		{
			return said;
		}
		heldForPlease = said;
		askPlease = true;
		return said;
	}

	internal static bool Wants(string said)
	{
		if (Regex.IsMatch(said ?? "", "\\b(please|pls|plz|stp)\\s*[.!?]*$", RegexOptions.IgnoreCase) && Site(Asked(said)))
		{
			return true;
		}
		string[] array = Regex.Split(said ?? "", "[,;!?，；！？。．،؟।።፣፧။၊։՝]\\s*|\\.\\s+");
		foreach (string said2 in array)
		{
			string input = Asked(WithoutMouthOrEyes(Asked(said2)));
			if (!Chatter.IsMatch(input) && Asks.IsMatch(input))
			{
				return true;
			}
		}
		return false;
	}

	internal static void Rescue(string said, List<KeyValuePair<string, ToolArguments>> calls)
	{
		string text = Asked(said);
		if (calls.Exists((KeyValuePair<string, ToolArguments> call) => call.Key == "use_computer" || call.Key == "look_at_screen" || call.Key == "media" || call.Key == "play_music"))
		{
			return;
		}
		if (Wants(said) && ((Errand(said) && !calls.Exists((KeyValuePair<string, ToolArguments> call) => call.Key == "shortcut" || call.Key == "remember")) || Regex.IsMatch(text, "\\s(and|then|&)\\s+(then\\s+)?(\\w+\\s+)?(search|look|type|click|go|make|create|write|find|open|sign|log|send|start|join|watch|check|put|add|draw|set|turn|say|tell|message|text|dm|reply|post|ask|paste|share|ping|install|uninstall|reinstall|download|upload|update|upgrade)\\b", RegexOptions.IgnoreCase)))
		{
			calls.Clear();
			calls.Add(new KeyValuePair<string, ToolArguments>("use_computer", new ToolArguments
			{
				Task = text
			}));
			return;
		}
		for (int num = 0; num < calls.Count; num++)
		{
			ToolArguments value = calls[num].Value;
			if (!(calls[num].Key != "open_website") && value != null && !string.IsNullOrEmpty(value.Site) && !Regex.IsMatch(text, "[a-z0-9-]+\\.[a-z]{2,}", RegexOptions.IgnoreCase))
			{
				string text2 = Regex.Replace(value.Site.Trim(), "^(https?://)?(www\\.)?|\\.[a-z.]+(/.*)?$", "", RegexOptions.IgnoreCase);
				if (Regex.IsMatch(text, "\\b" + Regex.Escape(text2) + "\\b", RegexOptions.IgnoreCase) && Installed(text2) && string.IsNullOrEmpty(value.Search))
				{
					calls[num] = new KeyValuePair<string, ToolArguments>("open_app", new ToolArguments
					{
						Name = text2
					});
				}
			}
		}
	}

	internal static ToolArguments Lookup(string said, string reply)
	{
		string text = Asked(said);
		string input = Regex.Replace(text, "^(do|did) (you|u) (know|have any idea)\\b|\\byou know\\b|^(tell|show|get|find) me\\b|\\b(near me|where i (am|live)|in my (area|city|town|suburb))\\b", "", RegexOptions.IgnoreCase);
		if (text.Length < 4 || !Question.IsMatch((said ?? "").Trim()) || !Unknown.IsMatch((reply ?? "").Replace('’', '\'')) || Regex.IsMatch(input, "\\b(screen|song|playing|track|you|your|yourself|u|ur|capri|me|my|i|i'm|im)\\b", RegexOptions.IgnoreCase) || Regex.IsMatch(reply ?? "", "\\bscreen\\b", RegexOptions.IgnoreCase))
		{
			return null;
		}
		ToolArguments toolArguments = new ToolArguments();
		toolArguments.Site = "google";
		toolArguments.Search = text;
		return toolArguments;
	}

	internal static string Pulled(string result)
	{
		if ((result ?? "").StartsWith("Opened your own website", StringComparison.Ordinal))
		{
			return "Here's my site, choom. That's where people download me.";
		}
		if (Regex.IsMatch(result ?? "", "^(Shutting your PC down|Restarting your PC|Signing you out|Putting your PC to sleep|Lock(ed|ing) your PC|Cancelled it\\.)"))
		{
			return result;
		}
		Match match = Regex.Match(result ?? "", "^Searched google\\.com for (.+)\\.$");
		if (!match.Success)
		{
			return null;
		}
		return "Pulled up \"" + match.Groups[1].Value + "\" on Google for you, choom.";
	}

	internal static bool Promised(string said)
	{
		return Promise.IsMatch(Warmth.Replace((said ?? "").Replace('’', '\''), ""));
	}

	internal static bool Credits(string said)
	{
		return Credit.IsMatch((said ?? "").Replace('’', '\''));
	}

	internal static bool Hesitant(string reply, string said)
	{
		string text = Asked(said);
		if (text.Length == 0 || Chatter.IsMatch(text) || !Doing.IsMatch(text))
		{
			return false;
		}
		string[] array = Regex.Split((reply ?? "").Replace('’', '\'').Trim(), "(?<=[.!?])\\s+");
		foreach (string input in array)
		{
			if (!Regex.IsMatch(input, "^\\W*(what|which|who|whom|whose|where|when|how|why)\\b", RegexOptions.IgnoreCase) && Hesitation.IsMatch(input))
			{
				return true;
			}
		}
		return false;
	}

	internal static bool Agrees(string said)
	{
		string text = Asked(said);
		if (text.Length > 0 && text.Length <= 60)
		{
			return Agreeing.IsMatch(text);
		}
		return false;
	}

	internal static KeyValuePair<string, ToolArguments> Forced(string said)
	{
		return new KeyValuePair<string, ToolArguments>("use_computer", new ToolArguments
		{
			Task = Asked(said)
		});
	}

	internal static bool Errand(string said)
	{
		string[] array = Regex.Split(said ?? "", "[,;!?，；！？。．،؟।።፣፧။၊։՝]\\s*|\\.\\s+");
		foreach (string said2 in array)
		{
			string input = Asked(said2);
			if (!Chatter.IsMatch(input) && Errands.IsMatch(input))
			{
				return true;
			}
		}
		return false;
	}

	internal static bool Follows(string said, string asked, string answered)
	{
		string text = Asked(said);
		if (Chatter.IsMatch(text) || Regex.IsMatch(text, "^(no|nah|never mind|nevermind|don't|do not|non|annule)\\b", RegexOptions.IgnoreCase))
		{
			return false;
		}
		if (Wants(text) && Regex.IsMatch(text, "\\b(it|this|that|them|there|again|instead|ça|celui)\\b|-(le|la)\\b", RegexOptions.IgnoreCase))
		{
			return true;
		}
		if (!Wants(asked))
		{
			return false;
		}
		if (Regex.IsMatch(text, "^(do (it|that)|same again|again|on (spotify|youtube)( instead)?|fais(-le| ça)|encore)$", RegexOptions.IgnoreCase))
		{
			return true;
		}
		if (!Regex.IsMatch(answered ?? "", "\\b(which|what should I play|lequel|laquelle)\\b|\\b(or|ou)\\b[^?]*\\?", RegexOptions.IgnoreCase))
		{
			return false;
		}
		if (Regex.IsMatch(text, "^(the )?(first|second|other|last)( one)?$|^(le|la) (premi[èe]re?|deuxi[èe]me|seconde?|derni[èe]re?|autre)$|^(yes|yeah|yep|oui)$", RegexOptions.IgnoreCase))
		{
			return true;
		}
		if (text.Length > 1 && text.Length < 80)
		{
			return Regex.IsMatch(answered ?? "", "(?<!\\w)" + Regex.Escape(text) + "(?!\\w)", RegexOptions.IgnoreCase);
		}
		return false;
	}

	internal static bool CantSee(string reply)
	{
		return Unseen.IsMatch(reply ?? "");
	}

	internal static bool Refused(string reply)
	{
		return Refusal.IsMatch(Dots((reply ?? "").Replace('’', '\'')));
	}

	private static string Dots(string text)
	{
		return text.Replace("\u103a\u1037", "\u1037\u103a");
	}

	internal static bool Requested(string said)
	{
		string input = (said ?? "").Trim();
		if (Regex.IsMatch(input, "[\\u0370-\\u03FF\\u1F00-\\u1FFF]"))
		{
			input = Regex.Replace(input, "[;\\u037E]\\s*$", "?");
		}
		if (!Regex.IsMatch(input, "[?？؟፧]\\s*$"))
		{
			return true;
		}
		input = Regex.Replace(Opener.Replace(input, ""), "^[\\s¿¡]+", "");
		if (!Regex.IsMatch(input, "^((hey|yo|ok|okay|oye|hola|hallo|ol[aá]|ciao|cze[śs][ćc]|hoi|привет)[\\s,]+)?(capri[\\s,]+)?((can|could|would|will) (you|u)|(me )?(puedes|podr[ií]as|pones|abres)|(est-ce que )?tu (peux|pourrais)|peux-tu|pourrais-tu|kannst du|k[öo]nntest du|w[üu]rdest du|(voc[eê] |vc )?(pode|poderia|consegue)s?|puoi|potresti|riesci a|mo[żz]esz|m[óo]g[łl]by[śs]|mog[łl]aby[śs]|(kun|kan|wil) je|можешь|можете|мог бы|могла бы|не могла бы|làm ơn|hãy|bạn có thể|tolong|bisa(kah)?|(kamu|kau|anda) bisa|ممكن|هل (يمكنك|تستطيع)|من فضلك|لو سمحت|можеш|чи можеш|чи можете|kan du|kunde du|skulle du|vil du|voisitko|voitko|můžeš|mohl bys|mohla bys|môžeš|((meg|el|be|le) )?tudnád|poți|poti|ai putea|μπορείς να|μπορεις να|θα μπορούσες να|pwede mo bang|puwede mo bang|pwede bang|kaya mo bang|boleh(kah)?|(je )?(unaweza|waweza)|tafadhali|naomba|(em )?(pots|podries)|možeš li|mozes li|da li možeš|da li mozes|možete li|mozete li|да ли можеш|kas (sa |te )?(saad|saate|võiksid|võiksite|saaksid|saaksite)|vai (tu )?(vari|varētu)|ar (gali|galėtum|galite)|(ali |a )?(mi )?lahko|bi (mi )?lahko|би ли|може ли|tista'?|tistax|tistgħu|podes|poder[ií]as|mesedez|kan jy|sal jy|geturðu|geturdu|gætirðu|gaetirdu|viltu|(a )?mund(esh)? (të|te|ta)|an (féidir|feidir) leat|alli di|allet ti|wnei di|fedri di|gallwch chi|wnewch chi|(ці )?можаш|(ці )?можаце|дали можеш|za (ka|ki) iya|(don|dan) allah|ṣé o lè|se o le|jọ\u0300wọ\u0301|jọwọ|jowo|biko|(i|ị) nwere ike|ungakwazi|ngicela|ndicela|sicela|fadlan|tulung|את יכולה|אתה יכול|תוכלי|תוכל|אפשר|בבקשה|می[\\u200C ]?(تونی|توانی)|می[\\u200C ]?شه|لطفا|لطفا\u064b|შეგიძლია|შეძლებ|გთხოვ|გთხოვთ|կարող ես|կարո՞ղ ես|կարող եք|կարո՞ղ եք|խնդրում եմ)(?!\\w)", RegexOptions.IgnoreCase) && !Regex.IsMatch(input, "^(capri[\\s,，]*)?((你|您)?(能不能|可不可以|能|可以|会不会|會不會)?(帮|幫|给|給|替)我|(你|您)(能|可以)|请|請|麻烦|麻煩|ช\u0e48วย|กร\u0e38ณา|รบกวน|ກະລ\u0eb8ນາ|ຊ\u0ec8ວຍ|ស\u17bcម|ជ\u17bdយ|က\u103b\u1031\u1038ဇ\u1030\u1038ပ\u103c\u102f၍|က\u103b\u1031\u1038ဇ\u1030\u1038ပ\u103c\u102fပ\u103c\u102e\u1038)", RegexOptions.IgnoreCase))
		{
			return Regex.IsMatch(input, "(くれる|くれない|くれます|くれませんか|もらえる|もらえない|もらえます|もらえませんか|ください|줄래|줄래요|줄 ?수 ?있어|줄 ?수 ?있어요|주세요|주실래요|줘|줘요|r m[ıi]s[ıi]n|bilir misin|सकत\u0947 ह\u094b|सकत\u0940 ह\u094b|सक\u094bग\u0947|सक\u094bग\u0940|सकत\u0947 ह\u0948\u0902|सकत\u0940 ह\u0948\u0902|سکتے ہو|سکتی ہو|سکتے ہیں|سکتی ہیں|سکو گے|سکو گی|প\u09beরব\u09c7|প\u09beরব\u09c7ন|প\u09beরব\u09bf|शकत\u0947स क\u093e|शकत\u094bस क\u093e|शकत\u093e क\u093e|शक\u093eल क\u093e|શક\u0ac7|શક\u0ac0શ|શક\u0acb|શકશ\u0acb|ਸਕਦ\u0a47 ਹ\u0a4b|ਸਕਦ\u0a40 ਹ\u0a4b|ਸਕਦ\u0a40 ਹ\u0a48\u0a02|ਸਕ\u0a4bਗ\u0a47|ਸਕ\u0a4bਗ\u0a40|ம\u0bc1ட\u0bbfய\u0bc1ம\u0bbe|గలవ\u0c3e|గలర\u0c3e|ಬಹ\u0cc1ದ\u0cbe|\u0d3eമ\u0d4b|പറ\u0d4dറ\u0d41മ\u0d4b|ได\u0e49ไหม|ได\u0e49ม\u0e31\u0e49ย|ได\u0e49ม\u0e31\u0e4aย|หน\u0e48อย|dezakezu|ahal duzu|al duzu|bilərsən|bilərsənmi|bilərsiniz|bilərsinizmi|olasanmi|olasizmi|berasanmi|berasizmi|аласың ба|аласыз ба|бересің бе|бересіз бе|чадах уу|өгөх үү|өгнө үү|өгөөч|सक\u094dछ\u094c|सक\u094dन\u0941ह\u0941न\u094dछ|सक\u094dछस\u094d|द\u093fन\u0941ह\u0941न\u094dछ|द\u093fन\u094dछ\u094c|kartaa|kartid|ትችላለህ|ትችያለሽ|ትችላላችሁ|ይችላሉ|ໄດ\u0ec9ບ\u0ecd|ໄດ\u0ec9ບ\u0ecd\u0ec8|ប\u17b6នទ\u17c1|ប\u17b6នអត\u17cb|န\u102d\u102fင\u103aမလ\u102c\u1038|န\u102d\u102fင\u103aလ\u102c\u1038|ပ\u1031\u1038မလ\u102c\u1038|လ\u102d\u102f\u1037 ?ရမလ\u102c\u1038|လ\u102d\u102f\u1037 ?ရလ\u102c\u1038|შეგიძლია|შეძლებ|կարո՞ղ ես|կարող ես|կարո՞ղ եք|կարող եք)\\s*(か)?\\s*[?？؟፧]\\s*$", RegexOptions.IgnoreCase);
		}
		return true;
	}

	private static string WithoutMouthOrEyes(string said)
	{
		if (!MouthOrEyes.IsMatch(said ?? ""))
		{
			return said;
		}
		return Regex.Replace(MouthOrEyes.Replace(said, " ").Trim(), "^[\\s,]*(and|then|i|y|e|et|und|и|і|та|ve|dan|và|en|och|og|ja|és|și|και|at|na|dhe|ac|a)\\s+", "", RegexOptions.IgnoreCase);
	}

	internal static string MovingFor(string text, out string screen, out string window)
	{
		screen = null;
		window = null;
		for (int i = 0; i < SelfWords.Length; i += 2)
		{
			Match match = Regex.Match(text ?? "", SelfWords[i], RegexOptions.IgnoreCase);
			if (match.Success)
			{
				if (match.Groups["s"].Success)
				{
					screen = (Regex.IsMatch(match.Groups["s"].Value, "^(here|over here|to me|to (my|the) (mouse|cursor|pointer)|to this (monitor|screen|display))$", RegexOptions.IgnoreCase) ? "here" : match.Groups["s"].Value.ToLowerInvariant());
				}
				if (match.Groups["w"].Success)
				{
					window = match.Groups["w"].Value.Trim();
				}
				return SelfWords[i + 1];
			}
		}
		return null;
	}

	internal static string AnimationFor(string text)
	{
		for (int i = 0; i < AnimationWords.Length; i += 2)
		{
			Match match = Regex.Match(text ?? "", AnimationWords[i], RegexOptions.IgnoreCase);
			if (!match.Success)
			{
				continue;
			}
			if (AnimationWords[i + 1].Length > 0)
			{
				return AnimationWords[i + 1];
			}
			string text2 = match.Groups["n"].Value.ToLowerInvariant();
			if (!(text2 == "smile"))
			{
				if (!(text2 == "dancing"))
				{
					return text2;
				}
				return "dance";
			}
			return "grin";
		}
		return null;
	}

	internal static int PickScreen(int[] x, int[] y, int[] numbers, int primary, int current, int pointed, string which)
	{
		int num = x.Length;
		if (num == 0)
		{
			return -1;
		}
		string input = Regex.Replace((which ?? "").ToLowerInvariant(), "\\b(the|my|your|ur|to|on|onto|(?:monitor|screen|display)s?|number|no)\\b|[#.]", " ");
		input = Regex.Replace(Regex.Replace(input, "\\s+", " ").Trim(), "(?<=\\w) one$", "");
		List<int> list = new List<int>();
		for (int i = 0; i < num; i++)
		{
			list.Add(i);
		}
		list.Sort((int a, int b) => (x[a] == x[b]) ? y[a].CompareTo(y[b]) : x[a].CompareTo(x[b]));
		if (Regex.IsMatch(input, "^(here|this|mouse|cursor|pointer)$"))
		{
			return pointed;
		}
		if (Regex.IsMatch(input, "^(main|primary)$"))
		{
			return primary;
		}
		if (input == "last")
		{
			return list[num - 1];
		}
		string[] array = new string[4] { "first|1st|one", "second|2nd|two", "third|3rd|three", "fourth|4th|four" };
		int num2 = (Regex.IsMatch(input, "^\\d$") ? (input[0] - 48) : 0);
		for (int num3 = 0; num3 < array.Length; num3++)
		{
			if (Regex.IsMatch(input, "^(" + array[num3] + ")$"))
			{
				num2 = num3 + 1;
			}
		}
		if (num2 > 0)
		{
			if (numbers != null)
			{
				for (int num4 = 0; num4 < num; num4++)
				{
					if (numbers[num4] == num2)
					{
						return num4;
					}
				}
			}
			if (num2 > num)
			{
				return -1;
			}
			return list[num2 - 1];
		}
		if (num < 2 || current < 0 || current >= num)
		{
			return -1;
		}
		int num5 = list.IndexOf(current);
		if (Regex.IsMatch(input, "^(previous|back)$"))
		{
			return list[(num5 + num - 1) % num];
		}
		Match match = Regex.Match(input, "^(?:(left)|(right)|(top|upper|up|above)|(bottom|lower|down|below))$");
		if (!match.Success)
		{
			return list[(num5 + 1) % num];
		}
		int result = -1;
		long num6 = long.MaxValue;
		for (int num7 = 0; num7 < num; num7++)
		{
			long num8 = x[num7] - x[current];
			long num9 = y[num7] - y[current];
			bool flag;
			if (match.Groups[1].Success)
			{
				flag = num8 < 0;
			}
			else if (match.Groups[2].Success)
			{
				flag = num8 > 0;
			}
			else
			{
				flag = (match.Groups[3].Success ? (num9 < 0) : (num9 > 0));
			}
			if (num7 != current && flag && num8 * num8 + num9 * num9 < num6)
			{
				num6 = num8 * num8 + num9 * num9;
				result = num7;
			}
		}
		return result;
	}

	internal static string Asked(string said)
	{
		said = Dots(Regex.Replace(said ?? "", "^[\\s¿¡]+", ""));
		string text = Lead.Replace(Opener.Replace(Regex.Replace((said ?? "").Replace('’', '\'').Trim(), "\\s+", " "), ""), "");
		for (int i = 0; i < 3; i++)
		{
			text = Regex.Replace(Regex.Replace(text, "[\\s,.!?;，；！？、。．…،؟।॥።፣፧။၊։՞՜՝;]+$", ""), "\\s+(for me|please|pls|plz|rn|right now|now|stp|s'il te pla[iî]t|sil te pla[iî]t|por favor|porfavor|porfa|porfis|bitte|bitte schön|per favore|perfavore|per piacere|proszę|prosze|пожалуйста|плз|пж|будь ласка|lütfen|lutfen|alsjeblieft|alstublieft|a\\.?u\\.?b|tolong|dong)$", "", RegexOptions.IgnoreCase);
		}
		return text;
	}

	internal static bool Loudness(string said, out string action, out string level, out bool spotify)
	{
		action = (level = null);
		string input = Regex.Replace((said ?? "").ToLowerInvariant().Replace('’', '\''), "^(?:(?:i (?:want|need) (?:you|u) to|go ahead and|just|quickly|quick)\\s+)+", "");
		spotify = Regex.IsMatch(input, "\\bspotify\\b");
		input = Regex.Replace(input, "\\s+(?:on|in|for)\\s+(?:spotify|youtube|my pc|the pc|my computer|the computer|my laptop)$", "");
		input = Regex.Replace(input, "(?<!all the )\\bway (up|down)$", "$1 a lot");
		string text = null;
		Match match = Regex.Match(input, "\\s+(?:(?<small>(?:just )?a (?:bit|little|lil|tad|notch|touch|smidge|hair)(?: more)?|a little bit(?: more)?|slightly)|(?<big>a lot(?: more)?|loads|heaps|tons|(?:a )?(?:bunch|ton)(?: more)?|much more)|by (?<n>\\d{1,3})\\s*(?:%|percent|per ?cent|por ?ciento|pour ?cent)?)$");
		if (match.Success)
		{
			if (match.Groups["small"].Success)
			{
				text = "6";
			}
			else
			{
				text = (match.Groups["big"].Success ? "20" : match.Groups["n"].Value);
			}
			input = input.Substring(0, match.Index);
		}
		for (int i = 0; i < MediaWords.Length; i += 3)
		{
			Match match2 = Regex.Match(input, MediaWords[i]);
			if (!match2.Success)
			{
				continue;
			}
			action = MediaWords[i + 1];
			bool flag = action == "volume_up" || action == "volume_down";
			if (text != null && !flag)
			{
				return false;
			}
			ref string reference = ref level;
			object obj;
			if (flag)
			{
				obj = text;
			}
			else if (MediaWords[i + 2] == "n")
			{
				obj = match2.Groups["n"].Value;
			}
			else
			{
				obj = ((MediaWords[i + 2].Length > 0) ? MediaWords[i + 2] : null);
			}
			reference = (string)obj;
			if (level != null)
			{
				if (int.TryParse(level, out var result))
				{
					return result <= 100;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	internal static void Aim(ToolArguments args, string said)
	{
		if (args != null)
		{
			if (args.Action == "mute" && Regex.IsMatch(said ?? "", "\\bun-?mute\\b|\\bmute off\\b|\\boff (of )?mute\\b|\\b(sound|volume|audio) (back )?on\\b|\\bturn on (the )?(sound|volume|audio)\\b", RegexOptions.IgnoreCase))
			{
				args.Action = "unmute";
			}
			if (args.App == null && (args.Action == "volume_up" || args.Action == "volume_down") && Regex.IsMatch(said ?? "", "\\bspotify\\b", RegexOptions.IgnoreCase))
			{
				args.App = "spotify";
			}
		}
	}

	internal static bool Heard(string said, out string tool, out ToolArguments args)
	{
		List<string> list = new List<string>();
		Match match = Regex.Match(said ?? "", "\\b[ck]app?r(?:i|ee|ie|y)\\b[\\s,.!?]*([^\\s,.!?].*)$", RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
		if (match.Success)
		{
			list.Add(match.Groups[1].Value);
		}
		string[] array = Regex.Split((said ?? "").Trim(), "(?<=[.!?])\\s+");
		if (array.Length > 1)
		{
			list.Add(array[array.Length - 1]);
		}
		list.Add(said ?? "");
		foreach (string item in list)
		{
			if (Direct(item, out tool, out args))
			{
				return true;
			}
		}
		tool = null;
		args = new ToolArguments();
		return false;
	}

	internal static bool Direct(string said, out string tool, out ToolArguments args)
	{
		tool = null;
		args = new ToolArguments();
		if (Moon.IsMatch(said ?? ""))
		{
			tool = "play_music";
			args.Song = "I Really Want to Stay at Your House";
			args.Kind = "song";
			return true;
		}
		string text = Asked(said);
		if (Chatter.IsMatch(text) || Regex.IsMatch(text, "\\b(if|unless|until|after|before|when) (i|you|we|it|the)\\b", RegexOptions.IgnoreCase))
		{
			return false;
		}
		string text2 = PowerFor(text, out var minutes);
		if (text2 != null)
		{
			tool = "power";
			args.Action = text2;
			args.Minutes = minutes;
			return true;
		}
		Match match = Napping.Match(text);
		if (match.Success)
		{
			tool = "nap";
			args.Action = (match.Groups["wake"].Success ? "wake" : "sleep");
			return true;
		}
		string text3 = MovingFor(text, out var screen, out var window);
		if (text3 != null && window != null && !Sittable(window))
		{
			text3 = null;
		}
		if (text3 != null)
		{
			tool = "move_yourself";
			args.Action = text3;
			args.Screen = screen;
			args.Window = window;
			return true;
		}
		string text4 = AnimationFor(text);
		if (text4 != null)
		{
			tool = "animate";
			args.Name = text4;
			return true;
		}
		string text5 = ShortcutFor(text);
		if (text5 != null)
		{
			tool = "shortcut";
			args.Name = text5;
			return true;
		}
		if (ScreenAsk.IsMatch(text) || ScreenQuestion.IsMatch(text))
		{
			tool = "look_at_screen";
			args.Focus = (ScreenQuestion.IsMatch(text) ? text : null);
			return true;
		}
		bool flag = Regex.IsMatch((said ?? "").Trim(), "\\?\\s*$");
		bool flag2 = Regex.IsMatch(Opener.Replace((said ?? "").Trim(), ""), "^((hey|yo|ok|okay)[\\s,]+)?(capri[\\s,]+)?(can|could|would|will) (you|u)\\b", RegexOptions.IgnoreCase);
		Match match2 = Regex.Match(text, "^(?:(?:i (?:want|need|would like) (?:you|u) to|you (?:should|gotta|need to))\\s+)?(?:remember|memori[sz]e|don't forget|dont forget|do not forget|keep in mind|note (?:down|that)|(?:write|jot|note|put) (?:it|that|this) down|(?:leave|make|take|save|add|write|store|create|keep|drop) (?:a |an |one |this |that |the )?(?:new |quick |little )?(?:memory|memories|note|notes)(?: (?:in|to|into|on) (?:your |ur |the )?(?:notes|memory|memories|me\\.?md|md)(?: file)?)?(?: (?:that|saying|about|of))?|(?:save|put|add|write|store|log|jot|keep)(?: (?:it|that|this))?(?: down)? (?:to|in|into|on) (?:your |ur |the )?(?:notes|memory|memories|me\\.?md|md|brain|head)(?: file)?)(?:\\s+(?:that|this))?[:,]?\\s+(.+)$", RegexOptions.IgnoreCase);
		if (match2.Success && (!flag || flag2) && !Regex.IsMatch(match2.Groups[1].Value, "^(me|us|when|what|how|why|who|where|if|it|to|in|on|for|with|using|the time|last time|our|we|you|stuff|things|anything|everything|something)\\b|^(of )?(that|this)$", RegexOptions.IgnoreCase))
		{
			tool = "remember";
			args.Text = match2.Groups[1].Value;
			return true;
		}
		if ((!flag || flag2) && Regex.IsMatch(text, "^(?:(?:i (?:want|need|would like) (?:you|u) to|you (?:should|gotta|need to))\\s+)?(?:remember|memori[sz]e|don't forget|dont forget|do not forget|keep in mind|note (?:down|that)|(?:write|jot|note|put) (?:it|that|this) down|(?:leave|make|take|save|add|write|store|create|keep|drop) (?:a |an |one |this |that |the )?(?:new |quick |little )?(?:memory|memories|note|notes)(?: (?:in|to|into|on) (?:your |ur |the )?(?:notes|memory|memories|me\\.?md|md)(?: file)?)?(?: (?:that|saying|about|of))?|(?:save|put|add|write|store|log|jot|keep)(?: (?:it|that|this))?(?: down)? (?:to|in|into|on) (?:your |ur |the )?(?:notes|memory|memories|me\\.?md|md|brain|head)(?: file)?)(?: of)?(?: (?:that|this|it))?$", RegexOptions.IgnoreCase))
		{
			tool = "remember";
			args.Text = "";
			return true;
		}
		match2 = Regex.Match(text, "^(?:i (?:want|need|would like) (?:you|u) to\\s+)?forget(?:\\s+(?:that|about|the note about|my note about|what i said about))?\\s+([^,;]+)", RegexOptions.IgnoreCase);
		if (match2.Success && !flag && !Regex.IsMatch(match2.Groups[1].Value.Trim(), "^(it|that|this|about (it|that|this)|i asked|i said|what i said|the last thing|me|you|everything i said)\\b", RegexOptions.IgnoreCase))
		{
			tool = "remember";
			args.Action = "forget";
			args.Text = match2.Groups[1].Value.Trim();
			return true;
		}
		if (Loudness(text, out var action, out var level, out var spotify))
		{
			tool = "media";
			args.Action = action;
			args.Level = level;
			if (spotify && (action == "volume_up" || action == "volume_down"))
			{
				args.App = "spotify";
			}
			return true;
		}
		Match match3 = Regex.Match(text, "^(?:open|launch|start|go to|fire up|load up)\\s+(spotify|youtube)\\s*(?:,|and|&)?\\s*(?:then\\s+)?(?:play|shuffle|put on|blast)\\s+(.+)$", RegexOptions.IgnoreCase);
		if (match3.Success)
		{
			text = "play " + match3.Groups[2].Value + " on " + match3.Groups[1].Value;
		}
		if (Regex.IsMatch(text, "\\b(and|then|but)\\s+(open|launch|play|close|quit|pause|don't|do not)\\b", RegexOptions.IgnoreCase))
		{
			return false;
		}
		match3 = Regex.Match(text, "^(open|launch|start|run|load up|boot up|fire up)\\s+(up\\s+)?(.+)$", RegexOptions.IgnoreCase);
		if (match3.Success)
		{
			string text6 = match3.Groups[3].Value.Trim();
			if (Regex.IsMatch(text6, "\\s(and|then|&)\\s|,\\s*\\S", RegexOptions.IgnoreCase))
			{
				return false;
			}
			if (Regex.IsMatch(text6, "^(your|ur|capri'?s|the capri)( own)? (web ?site|site|page|download page)$", RegexOptions.IgnoreCase))
			{
				tool = "open_website";
				args.Site = "lachydotmcg.github.io/capri-desktop";
				return true;
			}
			if (Installed(text6))
			{
				tool = "open_app";
				args.Name = text6;
				return true;
			}
			if (Site(text6))
			{
				tool = "open_website";
				args.Site = text6;
				return true;
			}
			return false;
		}
		match3 = Regex.Match(text, "^(?:google|look up)\\s+(.+)$", RegexOptions.IgnoreCase);
		if (match3.Success && !Regex.IsMatch(match3.Groups[1].Value, "^(it|this|that|something|stuff|for me)$", RegexOptions.IgnoreCase))
		{
			tool = "open_website";
			args.Site = "google";
			args.Search = match3.Groups[1].Value.Trim();
			return true;
		}
		if (Regex.IsMatch(text, "^(what('?s| is| are)|how('?s| is)|is it|will it|when('?s| is| does| do)|who won|did|check|get|tell me|show me|find)\\b.*\\b(weather|forecast|temperature|raining|snowing|news|headlines|scores?|stock price|price of|exchange rate|time (is it )?in|opening hours|open (today|now|tonight)|release date|showtimes|rain|snow|sunny|hot out|cold out)\\b|^(weather|forecast|news|headlines)\\b|^who (won|is winning)\\b", RegexOptions.IgnoreCase) && !Regex.IsMatch(Regex.Replace(text, "\\bnear me\\b", ""), "\\b(my|your|you|me|i)\\b", RegexOptions.IgnoreCase))
		{
			tool = "open_website";
			args.Site = "google";
			args.Search = text;
			return true;
		}
		match3 = Regex.Match(text, "^(?:play|shuffle|put on|blast)\\s+(.+?)(\\s+on\\s+(spotify|youtube))?$", RegexOptions.IgnoreCase);
		if (match3.Success)
		{
			string text7 = match3.Groups[1].Value.Trim();
			if (!match3.Groups[3].Success && Installed(text7))
			{
				tool = "open_app";
				args.Name = text7;
				return true;
			}
			if (Regex.IsMatch(text7, "^(with|along|around|a game|games?\\b|dead|nice|fair|it\\b|me\\b|us\\b|together|((a|any|some|random) )?playlists?$)", RegexOptions.IgnoreCase))
			{
				return false;
			}
			if (Playable.IsMatch(text7) || Nickname(text7) != null || (!match3.Groups[3].Success && Showing(text7)))
			{
				return false;
			}
			if (match3.Groups[3].Success)
			{
				args.App = match3.Groups[3].Value.ToLowerInvariant();
			}
			if (Regex.IsMatch(text7, "^(music$|some music$|((a|any|some|random) )?(song|track|tune|music)s?$|(something|anything)\\b(?!\\s+by\\s))", RegexOptions.IgnoreCase))
			{
				tool = "play_music";
				args.Kind = "song";
				args.Song = Pick();
				return true;
			}
			Match match4 = Regex.Match(text7, "^(?:(?:the|my|your)\\s+)?(.+?)\\s+(playlist|album)$", RegexOptions.IgnoreCase);
			Match match5 = Regex.Match(text7, "^(?:the\\s+)?(playlist|album)\\s+(.+)$", RegexOptions.IgnoreCase);
			Match match6 = Regex.Match(text7, "^(?:some(?:thing)?|anything|(?:(?:some|a|any)\\s+)?(?:songs?|music|tracks?))\\s+by\\s+(.+)$", RegexOptions.IgnoreCase);
			if (Regex.IsMatch(text7, "^((my|your|the)\\s+)?((liked|saved)\\s+songs|likes)(\\s+playlist)?$", RegexOptions.IgnoreCase))
			{
				args.Kind = "playlist";
				args.Song = "Liked Songs";
			}
			else if (match4.Success)
			{
				args.Kind = match4.Groups[2].Value.ToLowerInvariant();
				args.Song = match4.Groups[1].Value;
			}
			else if (match5.Success)
			{
				args.Kind = match5.Groups[1].Value.ToLowerInvariant();
				args.Song = match5.Groups[2].Value;
			}
			else if (match6.Success)
			{
				args.Kind = "artist";
				args.Song = match6.Groups[1].Value;
			}
			else
			{
				args.Kind = "song";
				args.Song = text7;
			}
			tool = "play_music";
			return true;
		}
		match3 = Regex.Match(text, "^(close|quit|exit|kill|shut down|switch to|go to|bring up|show me|minimi[sz]e)\\s+(.+)$", RegexOptions.IgnoreCase);
		if (!match3.Success || Regex.IsMatch(match3.Groups[2].Value, "^(it|this|that|them|this one|that one|everything|all|all of (it|them)|the window|this window|that window)$", RegexOptions.IgnoreCase) || !Showing(match3.Groups[2].Value))
		{
			return false;
		}
		string text8 = match3.Groups[1].Value.ToLowerInvariant();
		tool = "app_window";
		args.Name = match3.Groups[2].Value.Trim();
		ref string action2 = ref args.Action;
		string text9;
		if (text8.StartsWith("minimi", StringComparison.Ordinal))
		{
			text9 = "minimize";
		}
		else
		{
			text9 = (Regex.IsMatch(text8, "^(close|quit|exit|kill|shut down)$") ? "close" : "switch_to");
		}
		action2 = text9;
		return true;
	}

	internal static bool Failed(string result)
	{
		return Regex.IsMatch(result ?? "", "^(Which app|What should I play|No app called|More than one|That didn't work|Couldn't|There's no|That isn't a website|Which job|Should I |Got lost|Ran out of steps|You pressed Esc|You grabbed the mouse|Using the mouse and keyboard isn't)|isn't open\\.$|but (couldn't|it's still open)");
	}

	private static ToolArguments Pairs(string tool, string call)
	{
		ToolArguments toolArguments = new ToolArguments();
		string text = call.Substring(1, call.Length - 2);
		bool flag = false;
		foreach (Match item in ToolPair.Matches(text))
		{
			flag = true;
			ToolArguments args = toolArguments;
			string key = item.Groups[1].Value.ToLowerInvariant();
			string value;
			if (item.Groups[2].Success)
			{
				value = item.Groups[2].Value;
			}
			else
			{
				value = (item.Groups[3].Success ? item.Groups[3].Value : item.Groups[4].Value.Trim());
			}
			Set(args, key, value);
		}
		string text2 = text.Trim().Trim('"', '\'');
		if (!flag && text2.Length > 0)
		{
			string key2;
			switch (tool)
			{
			default:
				key2 = "name";
				break;
			case "remember":
				key2 = "text";
				break;
			case "use_computer":
				key2 = "task";
				break;
			case "play_music":
				key2 = "song";
				break;
			case "look_at_screen":
				key2 = "focus";
				break;
			case "media":
			case "power":
			case "nap":
			case "move_yourself":
				key2 = "action";
				break;
			case "open_website":
				key2 = "site";
				break;
			}
			Set(toolArguments, key2, text2);
		}
		return toolArguments;
	}

	private static void Set(ToolArguments args, string key, string value)
	{
		switch (key)
		{
		case "name":
			args.Name = value;
			break;
		case "site":
			args.Site = value;
			break;
		case "search":
			args.Search = value;
			break;
		case "action":
			args.Action = value;
			break;
		case "focus":
			args.Focus = value;
			break;
		case "song":
			args.Song = value;
			break;
		case "app":
			args.App = value;
			break;
		case "kind":
			args.Kind = value;
			break;
		case "task":
			args.Task = value;
			break;
		case "text":
			args.Text = value;
			break;
		case "minutes":
			args.Minutes = value;
			break;
		case "screen":
			args.Screen = value;
			break;
		}
	}

	internal static bool Typed(string reply, out string tool, out ToolArguments args, bool loose = false)
	{
		tool = null;
		args = new ToolArguments();
		string input = Brain.Visible(reply).Trim();
		Match match = Regex.Match(input, "\\A```(?:json)?\\s*\\n(.*?)\\s*```\\z", RegexOptions.IgnoreCase | RegexOptions.Singleline);
		if (match.Success)
		{
			input = match.Groups[1].Value.Trim();
		}
		input = Regex.Replace(input, "<\\|\"\\|>|<escape>", "\"");
		Match match2 = ToolText.Match(input);
		if (match2.Success)
		{
			args = Read(match2.Groups[2].Value);
		}
		else
		{
			if (!loose)
			{
				return false;
			}
			match2 = LooseText.Match(input);
			if (!match2.Success || Regex.IsMatch(input.Substring(0, match2.Index), "\\b(not|never|won't|can't|cannot|don't)\\b", RegexOptions.IgnoreCase))
			{
				return false;
			}
			args = Read(match2.Groups[2].Value) ?? Pairs(match2.Groups[1].Value, match2.Groups[2].Value);
		}
		if (!Valid(match2.Groups[1].Value, args))
		{
			return false;
		}
		tool = match2.Groups[1].Value;
		return true;
	}

	internal static List<KeyValuePair<string, ToolArguments>> Written(string reply)
	{
		List<KeyValuePair<string, ToolArguments>> list = new List<KeyValuePair<string, ToolArguments>>();
		string text = Regex.Replace(Brain.Visible(reply), "<\\|\"\\|>|<escape>", "\"");
		int num = 0;
		while (num < text.Length)
		{
			int num2 = text.IndexOf('{', num);
			int num3 = ((num2 < 0) ? (-1) : Closing(text, num2));
			if (num3 < 0)
			{
				break;
			}
			string text2 = text.Substring(num2, num3 - num2 + 1);
			Match match = NamedBefore.Match(text.Substring(num, num2 - num));
			string text3 = null;
			ToolArguments toolArguments = null;
			if (match.Success)
			{
				text3 = match.Groups[1].Value;
				toolArguments = Read(text2);
			}
			else
			{
				Match match2 = NamedInside.Match(text2);
				Match match3 = GivenInside.Match(text2);
				int num4 = (match3.Success ? (match3.Index + match3.Length - 1) : (-1));
				int num5 = ((num4 < 0) ? (-1) : Closing(text2, num4));
				if (match2.Success && num5 > 0)
				{
					text3 = match2.Groups[1].Value;
					toolArguments = Read(text2.Substring(num4, num5 - num4 + 1));
				}
			}
			if (text3 != null && Valid(text3, toolArguments))
			{
				list.Add(new KeyValuePair<string, ToolArguments>(text3, toolArguments));
			}
			num = num3 + 1;
		}
		return list;
	}

	private static int Closing(string text, int open)
	{
		int num = 0;
		bool flag = false;
		for (int i = open; i < text.Length; i++)
		{
			char c = text[i];
			if (flag)
			{
				switch (c)
				{
				case '\\':
					i++;
					break;
				case '"':
					flag = false;
					break;
				}
				continue;
			}
			switch (c)
			{
			case '"':
				flag = true;
				break;
			case '{':
				num++;
				break;
			case '}':
				if (--num == 0)
				{
					return i;
				}
				break;
			}
		}
		return -1;
	}

	internal static ToolArguments Read(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}
		ToolArguments toolArguments = Strict(json);
		if (toolArguments != null)
		{
			return toolArguments;
		}
		string text = Loose(json);
		if (!(text == json))
		{
			return Strict(text);
		}
		return null;
	}

	private static ToolArguments Strict(string json)
	{
		try
		{
			using XmlDictionaryReader xmlDictionaryReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json), XmlDictionaryReaderQuotas.Max);
			ToolArguments toolArguments = (ToolArguments)new DataContractJsonSerializer(typeof(ToolArguments)).ReadObject(xmlDictionaryReader);
			return xmlDictionaryReader.Read() ? null : toolArguments;
		}
		catch (SerializationException)
		{
			return null;
		}
		catch (XmlException)
		{
			return null;
		}
	}

	internal static string Loose(string json)
	{
		string text = (json ?? "").Replace("<|\"|>", "\"");
		StringBuilder stringBuilder = new StringBuilder();
		char c = ' ';
		for (int i = 0; i < text.Length; i++)
		{
			char c2 = text[i];
			if (c2 == '"')
			{
				int j;
				for (j = i + 1; j < text.Length && text[j] != '"'; j += ((text[j] != '\\') ? 1 : 2))
				{
				}
				stringBuilder.Append(text, i, Math.Min(j + 1, text.Length) - i);
				i = j;
				c = '"';
			}
			else if ((c == '{' || c == ',' || c == ':') && !char.IsWhiteSpace(c2) && "{}[],:".IndexOf(c2) < 0)
			{
				int k;
				for (k = i; k < text.Length && text[k] != ',' && text[k] != '}' && (c == ':' || text[k] != ':'); k++)
				{
				}
				stringBuilder.Append('"').Append(text.Substring(i, k - i).Trim().Replace("\\", "\\\\")
					.Replace("\"", "\\\"")).Append('"');
				i = k - 1;
				c = '"';
			}
			else
			{
				stringBuilder.Append(c2);
				if (!char.IsWhiteSpace(c2))
				{
					c = c2;
				}
			}
		}
		return stringBuilder.ToString();
	}

	internal static bool Valid(string tool, ToolArguments args)
	{
		if (args == null)
		{
			return false;
		}
		switch (tool)
		{
		case "open_app":
			return Named(args.Name);
		case "open_website":
			return Named(args.Site);
		case "look_at_screen":
			return true;
		case "media":
		{
			bool result = int.TryParse(args.Level, out var result2) && result2 >= 0 && result2 <= 100;
			if (args.Action == "volume_set")
			{
				return result;
			}
			if (Regex.IsMatch(args.Action ?? "", "\\A(play_pause|pause|resume|next|previous|volume_up|volume_down|mute|unmute|now_playing)\\z"))
			{
				if (!string.IsNullOrEmpty(args.Level))
				{
					return result;
				}
				return true;
			}
			return false;
		}
		case "app_window":
			if (Named(args.Name))
			{
				return Regex.IsMatch(args.Action ?? "", "\\A(switch_to|minimize|close)\\z");
			}
			return false;
		case "use_computer":
			return Named(args.Task);
		case "shortcut":
			return Regex.IsMatch(args.Name ?? "", "\\A(discord_mute|discord_deafen|screenshot|clip_that|record|game_bar|show_desktop|task_manager|clipboard|emoji|voice_typing|live_captions|narrator|magnifier|magnifier_off|color_filters|quick_settings)\\z");
		case "remember":
			if (Named(args.Text))
			{
				if (!string.IsNullOrEmpty(args.Action) && !(args.Action == "remember"))
				{
					return args.Action == "forget";
				}
				return true;
			}
			return false;
		case "power":
			if (Regex.IsMatch(args.Action ?? "", "\\A(shut_down|restart|sleep|lock|sign_out|cancel)\\z"))
			{
				if (!string.IsNullOrEmpty(args.Minutes))
				{
					return Regex.IsMatch(args.Minutes.Trim(), "\\A\\d{1,4}\\z");
				}
				return true;
			}
			return false;
		case "nap":
			if (!string.IsNullOrEmpty(args.Action) && !(args.Action == "sleep"))
			{
				return args.Action == "wake";
			}
			return true;
		case "move_yourself":
			if (Regex.IsMatch(args.Action ?? "", "\\A(come_here|other_screen|sit|sit_on|lie_down|sit_up|stand_up|walk_around|wander|hang|ride_cursor|hop_off|hide|show|move_mainframe)\\z"))
			{
				if (!(args.Action != "sit_on"))
				{
					return Named(args.Window);
				}
				return true;
			}
			return false;
		case "animate":
			return Regex.IsMatch(args.Name ?? "", "\\A(idle|glitch|grin|blink|dance|stop_dancing)\\z");
		case "play_music":
			if (Named(args.Song) && (string.IsNullOrEmpty(args.Kind) || Regex.IsMatch(args.Kind, "\\A(song|playlist|album|artist)\\z")))
			{
				if (!string.IsNullOrEmpty(args.App) && !(args.App == "spotify"))
				{
					return args.App == "youtube";
				}
				return true;
			}
			break;
		}
		return false;
	}

	internal static bool Several(string said, List<KeyValuePair<string, ToolArguments>> calls)
	{
		Match match = Regex.Match(Asked(said), "^(?:open|launch|start|run|load up|boot up|fire up)\\s+(?:up\\s+)?(.+)$", RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			return false;
		}
		string[] array = Regex.Split(match.Groups[1].Value.Trim(), "\\s*,\\s*(?:and\\s+|&\\s*)?|\\s+and\\s+|\\s*&\\s*", RegexOptions.IgnoreCase);
		if (array.Length < 2)
		{
			return false;
		}
		List<KeyValuePair<string, ToolArguments>> list = new List<KeyValuePair<string, ToolArguments>>();
		string[] array2 = array;
		foreach (string text in array2)
		{
			string text2 = Regex.Replace(text.Trim(), "^(?:open|launch|start|run)\\s+", "", RegexOptions.IgnoreCase);
			if (Installed(text2))
			{
				list.Add(new KeyValuePair<string, ToolArguments>("open_app", new ToolArguments
				{
					Name = text2
				}));
				continue;
			}
			if (Site(text2))
			{
				list.Add(new KeyValuePair<string, ToolArguments>("open_website", new ToolArguments
				{
					Site = text2
				}));
				continue;
			}
			return false;
		}
		calls.AddRange(list);
		return true;
	}

	private static bool Named(string text)
	{
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim() != "...";
		}
		return false;
	}

	internal static bool Installed(string target)
	{
		string text = Regex.Replace(Plain(target), "^the |( app| application| game)$", "");
		if (text.Length == 0)
		{
			return false;
		}
		try
		{
			foreach (InstalledApp item in Apps())
			{
				if (Score(item, text) >= 3)
				{
					return true;
				}
			}
		}
		catch (Exception)
		{
		}
		return false;
	}

	private static bool Site(string target)
	{
		string text = target.Trim().ToLowerInvariant();
		if (Regex.IsMatch(text, "^(https?://)?[a-z0-9-]+(\\.[a-z0-9-]+)+(/\\S*)?$"))
		{
			return true;
		}
		for (int i = 0; i < Searches.GetLength(0); i++)
		{
			if (text == Searches[i, 0])
			{
				return true;
			}
		}
		return false;
	}

	private static bool Showing(string target)
	{
		string text = Regex.Replace(Plain(target), "^the |( app| application| game| tab| window)$", "");
		if (text.Length == 0)
		{
			return false;
		}
		try
		{
			foreach (OpenWindow item in Windows())
			{
				if (Math.Max(Score(Plain(item.Program), text), Score(Plain(item.Title), text)) > 0)
				{
					return true;
				}
			}
		}
		catch (Exception)
		{
		}
		return false;
	}

	internal static string Nickname(string wanted)
	{
		string text = Regex.Replace(Plain(wanted), "^the | (app|game)$", "");
		for (int i = 0; i < Nicknames.GetLength(0); i++)
		{
			if (text == Nicknames[i, 0])
			{
				return Nicknames[i, 1];
			}
		}
		return null;
	}

	private static string OpenApp(string wanted)
	{
		if (string.IsNullOrWhiteSpace(wanted))
		{
			return "Which app, choom?";
		}
		List<InstalledApp> list = Match(Apps(), wanted);
		string text = ((list.Count == 0) ? Nickname(wanted) : null);
		if (text != null)
		{
			list = Match(Apps(), text);
		}
		if (list.Count == 0)
		{
			return "No app called " + wanted.Trim() + " is installed on this PC.";
		}
		InstalledApp installedApp = list[0];
		foreach (InstalledApp item in list)
		{
			if (AppName(item).Length < AppName(installedApp).Length)
			{
				installedApp = item;
			}
		}
		Run(installedApp.Id);
		return "Opened " + ((AppName(installedApp) == "spotify") ? "Spotify" : installedApp.Name) + ".";
	}

	internal static List<InstalledApp> Match(List<InstalledApp> apps, string wanted)
	{
		string text = Regex.Replace(Plain(wanted), "^the |( app| application| game)$", "");
		List<InstalledApp> list = new List<InstalledApp>();
		if (text.Length == 0)
		{
			return list;
		}
		int num = 0;
		foreach (InstalledApp app in apps)
		{
			string name = AppName(app);
			int num2 = Score(app, text);
			if (num2 != 0 && num2 >= num)
			{
				if (num2 > num)
				{
					num = num2;
					list.Clear();
				}
				if (!list.Exists((InstalledApp other) => AppName(other) == name))
				{
					list.Add(app);
				}
			}
		}
		return list;
	}

	private static int Score(string name, string want)
	{
		if (name == want || name.Replace(" ", "") == want.Replace(" ", ""))
		{
			return 4;
		}
		if (name.StartsWith(want + " ", StringComparison.Ordinal))
		{
			return 3;
		}
		if ((" " + name + " ").Contains(" " + want + " "))
		{
			return 2;
		}
		if (want.Length >= 4 && name.Contains(want))
		{
			return 1;
		}
		return 0;
	}

	private static int Score(InstalledApp app, string want)
	{
		string text = AppName(app);
		if (text.Length != 0)
		{
			return Math.Max(Score(text, want), Score(Plain(app.Name), want));
		}
		return 0;
	}

	private static string AppName(InstalledApp app)
	{
		string text = Plain(app.Name);
		if (string.IsNullOrWhiteSpace(app.Id) || Junk.IsMatch(text) || app.Id.StartsWith("http", StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}
		if (Regex.IsMatch(app.Id, "^SpotifyAB\\.SpotifyMusic_[a-z0-9]+!Spotify$|^(?:[A-Za-z]:\\\\|\\\\\\\\).*[\\\\]Spotify\\.exe$", RegexOptions.IgnoreCase))
		{
			return "spotify";
		}
		return text;
	}

	private static string Plain(string text)
	{
		return Regex.Replace(Regex.Replace((text ?? "").Normalize(NormalizationForm.FormD).ToLowerInvariant(), "\\p{M}", ""), "[^\\p{L}\\p{Nd}]+", " ").Trim();
	}

	private static List<InstalledApp> Apps()
	{
		lock (gate)
		{
			if (cached != null && DateTime.UtcNow - cachedAt <= TimeSpan.FromMinutes(2.0))
			{
				return cached;
			}
			Exception ex = null;
			List<InstalledApp> list;
			try
			{
				list = new List<InstalledApp>(Catalog());
			}
			catch (Exception ex2)
			{
				list = new List<InstalledApp>();
				ex = ex2;
			}
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string text = ((folderPath.Length == 0) ? "" : Path.Combine(folderPath, "Spotify\\Spotify.exe"));
			if (text.Length > 0 && Exists(text) && !list.Exists((InstalledApp app) => AppName(app) == "spotify"))
			{
				list.Add(new InstalledApp("Spotify", text));
			}
			if (ex != null)
			{
				if (list.Count == 0)
				{
					throw ex;
				}
				return list;
			}
			cached = list;
			cachedAt = DateTime.UtcNow;
			return cached;
		}
	}

	private static List<InstalledApp> ReadCatalog()
	{
		List<InstalledApp> apps = new List<InstalledApp>();
		Exception failed = null;
		Thread thread = new Thread(() =>
		{
			try
			{
				object target = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
				object target2 = Call(Call(target, "NameSpace", "shell:AppsFolder"), "Items");
				int num = (int)Get(target2, "Count");
				for (int i = 0; i < num; i++)
				{
					object target3 = Call(target2, "Item", i);
					string text = Get(target3, "Name") as string;
					string text2 = Get(target3, "Path") as string;
					if (!string.IsNullOrEmpty(text2))
					{
						apps.Add(new InstalledApp(text ?? "", text2));
					}
				}
			}
			catch (Exception ex)
			{
				failed = ex;
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.IsBackground = true;
		thread.Start();
		if (!thread.Join(10000))
		{
			throw new TimeoutException("The app list took too long.");
		}
		if (failed != null)
		{
			throw failed;
		}
		return WithSteam(apps, SteamApps(SteamFolder()));
	}

	internal static List<InstalledApp> WithSteam(List<InstalledApp> apps, List<InstalledApp> steam)
	{
		foreach (InstalledApp game in steam)
		{
			if (AppName(game).Length > 0 && !apps.Exists((InstalledApp app) => AppName(app) == AppName(game)))
			{
				apps.Add(game);
			}
		}
		return apps;
	}

	internal static List<InstalledApp> SteamApps(string steam)
	{
		List<InstalledApp> list = new List<InstalledApp>();
		if (string.IsNullOrEmpty(steam))
		{
			return list;
		}
		try
		{
			List<string> list2 = new List<string>();
			list2.Add(steam);
			List<string> list3 = list2;
			string path = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
			if (File.Exists(path))
			{
				foreach (Match item in Regex.Matches(File.ReadAllText(path), "\"path\"\\s+\"([^\"]+)\""))
				{
					list3.Add(item.Groups[1].Value.Replace("\\\\", "\\"));
				}
			}
			HashSet<string> hashSet = new HashSet<string>();
			foreach (string item2 in list3)
			{
				string path2 = Path.Combine(item2, "steamapps");
				if (!Directory.Exists(path2))
				{
					continue;
				}
				string[] files = Directory.GetFiles(path2, "appmanifest_*.acf");
				foreach (string path3 in files)
				{
					string input;
					try
					{
						input = File.ReadAllText(path3);
					}
					catch (IOException)
					{
						continue;
					}
					catch (UnauthorizedAccessException)
					{
						continue;
					}
					Match match2 = Regex.Match(input, "\"appid\"\\s+\"(\\d+)\"");
					Match match3 = Regex.Match(input, "\"name\"\\s+\"([^\"]+)\"");
					Match match4 = Regex.Match(input, "\"StateFlags\"\\s+\"(\\d+)\"");
					if (match2.Success && match3.Success && hashSet.Add(match2.Groups[1].Value) && (!match4.Success || !long.TryParse(match4.Groups[1].Value, out var result) || (result & 4) != 0) && !Regex.IsMatch(match3.Groups[1].Value, "^(Steamworks Common Redistributables|Steam Linux Runtime\\b.*|Proton\\b.*)$", RegexOptions.IgnoreCase))
					{
						list.Add(new InstalledApp(match3.Groups[1].Value, "steam://rungameid/" + match2.Groups[1].Value));
					}
				}
			}
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
		return list;
	}

	private static string SteamFolder()
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam");
			string text = ((registryKey == null) ? null : (registryKey.GetValue("SteamPath") as string));
			return string.IsNullOrEmpty(text) ? null : text.Replace('/', '\\');
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static object Call(object target, string method, params object[] args)
	{
		return target.GetType().InvokeMember(method, BindingFlags.InvokeMethod, null, target, args);
	}

	private static object Get(object target, string property)
	{
		return target.GetType().InvokeMember(property, BindingFlags.GetProperty, null, target, null);
	}

	private static void Start(string id)
	{
		using (Process.Start(Launch(id)))
		{
		}
	}

	internal static ProcessStartInfo Launch(string id)
	{
		ProcessStartInfo processStartInfo = (Regex.IsMatch(id, "^[A-Za-z]:\\\\|^\\\\\\\\|^[a-z][a-z0-9+.-]*://|^spotify:", RegexOptions.IgnoreCase) ? new ProcessStartInfo(id) : new ProcessStartInfo("explorer.exe", "shell:AppsFolder\\" + id));
		processStartInfo.UseShellExecute = true;
		return processStartInfo;
	}

	private static string OpenSite(string site, string search)
	{
		string text = Address(site, search);
		if (text == null)
		{
			return "That isn't a website.";
		}
		Run(text);
		if (text.TrimEnd('/') == "https://lachydotmcg.github.io/capri-desktop")
		{
			return "Opened your own website, lachydotmcg.github.io/capri-desktop.";
		}
		string text2 = new Uri(text).Host;
		if (text2.StartsWith("www.", StringComparison.Ordinal))
		{
			text2 = text2.Substring(4);
		}
		if (!string.IsNullOrWhiteSpace(search))
		{
			return "Searched " + text2 + " for " + search.Trim() + ".";
		}
		return "Opened " + text2 + ".";
	}

	internal static string Address(string site, string search)
	{
		string text = Regex.Replace((site ?? "").Trim().ToLowerInvariant(), "^(https?://)?(www\\.)?", "");
		if (!string.IsNullOrWhiteSpace(search))
		{
			for (int i = 0; i < Searches.GetLength(0); i++)
			{
				string text2 = Searches[i, 0];
				if (text == text2 || text.StartsWith(text2 + ".", StringComparison.Ordinal) || text.Contains("." + text2 + "."))
				{
					return Searches[i, 1] + Uri.EscapeDataString(search.Trim());
				}
			}
			return Searches[1, 1] + Uri.EscapeDataString((search.Trim() + " " + text).Trim());
		}
		if (text.Length == 0)
		{
			return null;
		}
		if (text.IndexOf('.') < 0)
		{
			text += ".com";
		}
		if (!Uri.TryCreate("https://" + text, UriKind.Absolute, out var result) || result.Host.IndexOf('.') < 0 || result.UserInfo.Length > 0)
		{
			return null;
		}
		return result.AbsoluteUri;
	}

	private static string Media(ToolArguments args)
	{
		string action = args.Action;
		switch (action)
		{
		case "play_pause":
		case "pause":
		case "resume":
			return Toggle(action);
		case "next":
			Press(176);
			return "Skipped to the next track.";
		case "previous":
			Press(177);
			return "Went back a track.";
		default:
		{
			bool flag = int.TryParse(args.Level, out var result) && result >= 0 && result <= 100;
			switch (action)
			{
			case "volume_up":
			case "volume_down":
			{
				bool flag4 = action == "volume_up";
				int num2 = ((flag && result > 0) ? Math.Max(1, (result + 1) / 2) : 5);
				for (int i = 0; i < num2; i++)
				{
					Press((byte)(flag4 ? 175 : 174));
				}
				if (!flag4)
				{
					return "Turned the volume down.";
				}
				return "Turned the volume up.";
			}
			case "volume_set":
				if (!flag)
				{
					return "Couldn't set the volume: it goes from 0 to 100.";
				}
				if (!SetVolume((double)result / 100.0))
				{
					return "Couldn't set the volume: Windows didn't let me.";
				}
				if (result > 0 && MuteState() == true)
				{
					SetMute(arg: false);
				}
				return "Set the volume to " + result + "%.";
			case "mute":
			case "unmute":
			{
				bool flag2 = action == "mute";
				bool? flag3 = MuteState();
				if (!flag3.HasValue)
				{
					Press(173);
					return "Toggled mute.";
				}
				double? num = VolumeNow();
				if (!flag2 && num.HasValue && num.Value < 0.01 && SetVolume(0.2))
				{
					if (flag3.Value)
					{
						SetMute(arg: false);
					}
					return "Turned the sound back on at 20%.";
				}
				if (flag3.Value == flag2)
				{
					if (!flag2)
					{
						return "The sound is already on.";
					}
					return "The PC is already muted.";
				}
				if (!SetMute(flag2))
				{
					Press(173);
					return "Toggled mute.";
				}
				if (!flag2)
				{
					return "Unmuted the PC.";
				}
				return "Muted the PC.";
			}
			case "now_playing":
				return Playing();
			default:
				return "There's no media action called " + action + ".";
			}
		}
		}
	}

	private static string Playing()
	{
		string text = NowPlaying();
		if (TrackTitle(text))
		{
			int num = text.IndexOf(" - ", StringComparison.Ordinal);
			return "Spotify is playing " + ((num > 0) ? (text.Substring(num + 3) + " by " + text.Substring(0, num)) : text) + ".";
		}
		foreach (OpenWindow item in Windows())
		{
			Match match = Regex.Match(item.Title, "^(?:\\(\\d+\\)\\s*)?(.+?) - YouTube( Music)?( - |$)");
			if (match.Success)
			{
				return "Their YouTube tab is on " + match.Groups[1].Value + ".";
			}
		}
		if (Regex.IsMatch(text, "^Spotify( \\w+)?$"))
		{
			return "Spotify is open but paused.";
		}
		if (text.Length <= 0)
		{
			return "Nothing's playing that I can see.";
		}
		return "Spotify is open, but I can't tell what's playing.";
	}

	private static string Toggle(string action)
	{
		string text = NowPlaying();
		Press(179);
		for (int i = 0; i < 6; i++)
		{
			if (text.Length <= 0)
			{
				break;
			}
			Thread.Sleep(250);
			string text2 = NowPlaying();
			if (!(text2 == text))
			{
				if (TrackTitle(text2))
				{
					return "Spotify is playing again.";
				}
				if (TrackTitle(text))
				{
					return "Paused Spotify.";
				}
			}
		}
		if (!(action == "pause"))
		{
			if (!(action == "resume"))
			{
				return "Pressed play/pause.";
			}
			return "Pressed play.";
		}
		return "Pressed pause.";
	}

	internal static string ShortcutFor(string text)
	{
		string input = (text ?? "").Trim().ToLowerInvariant();
		for (int i = 0; i < ShortcutWords.Length; i += 2)
		{
			if (Regex.IsMatch(input, ShortcutWords[i]))
			{
				return ShortcutWords[i + 1];
			}
		}
		return null;
	}

	private static string Shortcut(string name)
	{
		int num = -1;
		for (int i = 0; i < Shortcuts.GetLength(0); i++)
		{
			if (Shortcuts[i, 0] == name)
			{
				num = i;
			}
		}
		if (num < 0)
		{
			return "There's no shortcut called " + name + ".";
		}
		string text = Instead(name);
		if (text != null)
		{
			return text;
		}
		string[] array = Shortcuts[num, 2].Split(' ');
		byte[] array2 = new byte[array.Length];
		for (int j = 0; j < array.Length; j++)
		{
			array2[j] = Convert.ToByte(array[j], 16);
		}
		if (Shortcuts[num, 1] != "discord" || !Presses)
		{
			Chord(array2);
			return Shortcuts[num, 3];
		}
		OpenWindow openWindow = FindDiscord();
		if (openWindow == null)
		{
			string text2 = OpenApp("Discord");
			if (!text2.StartsWith("Opened", StringComparison.Ordinal))
			{
				return "Discord isn't open, so I couldn't press its shortcut. " + text2;
			}
			for (int k = 0; k < 20; k++)
			{
				if (openWindow != null)
				{
					break;
				}
				Thread.Sleep(250);
				openWindow = FindDiscord();
			}
			if (openWindow == null)
			{
				return "Opened Discord, but its window didn't show up in time. Ask again in a sec.";
			}
		}
		IntPtr foregroundWindow = Native.GetForegroundWindow();
		if (!Steer(openWindow, "switch_to"))
		{
			return "Couldn't bring Discord to the front to press its shortcut.";
		}
		Chord(array2);
		Thread.Sleep(150);
		if (foregroundWindow != IntPtr.Zero && foregroundWindow != openWindow.Handle)
		{
			Steer(new OpenWindow(foregroundWindow, "", ""), "switch_to");
		}
		return Shortcuts[num, 3];
	}

	private static OpenWindow FindDiscord()
	{
		foreach (OpenWindow item in Windows())
		{
			if (string.Equals(item.Program, "Discord", StringComparison.OrdinalIgnoreCase) || string.Equals(item.Program, "DiscordPTB", StringComparison.OrdinalIgnoreCase) || string.Equals(item.Program, "DiscordCanary", StringComparison.OrdinalIgnoreCase))
			{
				return item;
			}
		}
		return null;
	}

	[DllImport("user32.dll")]
	private static extern bool LockWorkStation();

	[DllImport("user32.dll")]
	private static extern bool ExitWindowsEx(int flags, int reason);

	[DllImport("powrprof.dll")]
	private static extern bool SetSuspendState(bool hibernate, bool force, bool wakeEventsOff);

	private static string PowerFor(string text, out string minutes)
	{
		minutes = null;
		if ((DateTime.Now - powered).TotalSeconds < 20.0 && Regex.IsMatch(text, "^(no+|nah|nope|wait|stop|cancel|abort|undo|don'?t|do not|never ?mind|hold on|hold up)\\b", RegexOptions.IgnoreCase))
		{
			return "cancel";
		}
		Match match = Regex.Match(text, "\\s+in\\s+(\\d{1,4}|an?|one)\\s*(min(ute)?s?|h(ou)?rs?)$", RegexOptions.IgnoreCase);
		string input = (match.Success ? text.Substring(0, match.Index) : text);
		for (int i = 0; i < PowerWords.Length; i += 2)
		{
			if (Regex.IsMatch(input, PowerWords[i], RegexOptions.IgnoreCase))
			{
				if (match.Success)
				{
					int num = (Regex.IsMatch(match.Groups[1].Value, "^(an?|one)$", RegexOptions.IgnoreCase) ? 1 : int.Parse(match.Groups[1].Value));
					minutes = (match.Groups[2].Value.StartsWith("h", StringComparison.OrdinalIgnoreCase) ? (num * 60) : num).ToString();
				}
				return PowerWords[i + 1];
			}
		}
		return null;
	}

	private static string Turn(string action, string minutes)
	{
		if (!int.TryParse((minutes ?? "").Trim(), out var result) || result < 0)
		{
			result = 0;
		}
		result = Math.Min(result, 1440);
		if (action == "cancel")
		{
			powered = DateTime.MinValue;
			if (!Power("cancel", 0))
			{
				return "Nothing was shutting down, so there's nothing to cancel.";
			}
			return "Cancelled it. Your PC stays on.";
		}
		int num;
		if (result <= 0)
		{
			switch (action)
			{
			default:
				num = 0;
				break;
			case "sleep":
				num = 4;
				break;
			case "sign_out":
				num = 5;
				break;
			case "shut_down":
			case "restart":
				num = 10;
				break;
			}
		}
		else
		{
			num = result * 60;
		}
		int arg = num;
		if (!Power(action, arg))
		{
			return action switch
			{
				"lock" => "Couldn't lock your PC.", 
				"sleep" => "Couldn't put your PC to sleep.", 
				"restart" => "Couldn't restart your PC.", 
				"shut_down" => "Couldn't shut your PC down.", 
				_ => "Couldn't sign you out.", 
			};
		}
		if (result == 0 && action != "lock")
		{
			powered = DateTime.Now;
		}
		string text;
		if (result != 0)
		{
			text = ((result % 60 == 0) ? (result / 60 + ((result == 60) ? " hour" : " hours")) : (result + ((result == 1) ? " minute" : " minutes")));
		}
		else
		{
			text = null;
		}
		string text2 = ((text == null) ? " Say cancel to stop it." : " Say cancel the shutdown to stop it.");
		switch (action)
		{
		case "shut_down":
			return "Shutting your PC down in " + (text ?? "10 seconds") + "." + text2;
		case "restart":
			return "Restarting your PC in " + (text ?? "10 seconds") + "." + text2;
		case "sign_out":
			return "Signing you out in " + (text ?? "5 seconds") + "." + text2;
		case "sleep":
			if (text != null)
			{
				return "Putting your PC to sleep in " + text + ".";
			}
			return "Putting your PC to sleep.";
		default:
			if (text != null)
			{
				return "Locking your PC in " + text + ".";
			}
			return "Locked your PC.";
		}
	}

	private static bool Switch(string action, int seconds)
	{
		if (action == "cancel")
		{
			bool flag;
			lock (gate)
			{
				flag = powerLater != null;
				if (flag)
				{
					powerLater.Dispose();
				}
				powerLater = null;
			}
			if (!Shutdown("/a"))
			{
				return flag;
			}
			return true;
		}
		if (action == "shut_down" || action == "restart")
		{
			return Shutdown(((action == "restart") ? "/r" : "/s") + " /t " + seconds);
		}
		if (action == "lock" && seconds == 0)
		{
			return LockWorkStation();
		}
		Timer timer = null;
		timer = new Timer((object param0) =>
		{
			lock (gate)
			{
				if (powerLater != timer)
				{
					return;
				}
				powerLater = null;
			}
			if (action == "lock")
			{
				LockWorkStation();
			}
			else if (action == "sign_out")
			{
				ExitWindowsEx(0, 0);
			}
			else if (!SetSuspendState(hibernate: false, force: false, wakeEventsOff: false))
			{
				PostMessage(new IntPtr(65535), 274, new IntPtr(61808), new IntPtr(2));
			}
		}, null, -1, -1);
		lock (gate)
		{
			if (powerLater != null)
			{
				powerLater.Dispose();
			}
			powerLater = timer;
		}
		timer.Change(seconds * 1000, -1);
		return true;
	}

	private static bool Shutdown(string flags)
	{
		try
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "shutdown.exe"), flags);
			processStartInfo.UseShellExecute = false;
			processStartInfo.CreateNoWindow = true;
			using Process process = Process.Start(processStartInfo);
			return process != null && process.WaitForExit(5000) && process.ExitCode == 0;
		}
		catch (Win32Exception)
		{
			return false;
		}
	}

	private static void PressChord(byte[] keys)
	{
		foreach (byte b in keys)
		{
			Native.keybd_event(b, 0, (b == 91) ? 1 : 0, UIntPtr.Zero);
		}
		for (int num = keys.Length - 1; num >= 0; num--)
		{
			Native.keybd_event(keys[num], 0, ((keys[num] == 91) ? 1 : 0) | 2, UIntPtr.Zero);
		}
	}

	private static void Tap(byte key)
	{
		Native.keybd_event(key, 0, 1, UIntPtr.Zero);
		Native.keybd_event(key, 0, 3, UIntPtr.Zero);
	}

	internal static string Pick()
	{
		return Ost[Roll(Ost.Length)];
	}

	internal static bool NightCity(string title)
	{
		if (!string.IsNullOrEmpty(title))
		{
			if (!NightCityMusic.IsMatch(title))
			{
				if (NightCityName.IsMatch(title))
				{
					return MusicWord.IsMatch(title);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private static string PlayMusic(string song, string app, string kind)
	{
		if (string.IsNullOrWhiteSpace(song))
		{
			return "What should I play?";
		}
		song = song.Trim();
		if (Regex.IsMatch(song, "^((what('|\\u2019)?s|what is)\\s+)?((currently|now)\\s+)?playing( now)?$|^(this|that|the current) (song|track)$", RegexOptions.IgnoreCase))
		{
			return Playing();
		}
		if (Regex.IsMatch(song, "^(play\\s+)?(me\\s+)?((some|any|a|random|good)\\s+)?(music|songs?|tunes|tracks?|something)(\\s+for me)?$", RegexOptions.IgnoreCase))
		{
			song = Pick();
			kind = "song";
		}
		Match match = Regex.Match(song, "^(?:(?:the|my|your)\\s+)?(.+?)\\s+(playlist|album)$", RegexOptions.IgnoreCase);
		if (match.Success && kind != "artist")
		{
			kind = match.Groups[2].Value.ToLowerInvariant();
			song = match.Groups[1].Value;
		}
		bool flag = Regex.IsMatch(song, "^((my|your|the)\\s+)?((liked|saved)\\s+songs|likes)$", RegexOptions.IgnoreCase);
		if (flag)
		{
			kind = "playlist";
		}
		if (kind != "playlist" && kind != "album" && kind != "artist")
		{
			kind = "song";
		}
		string text = null;
		string stringToEscape = song;
		if (kind == "song")
		{
			for (int i = 0; i < KnownSongs.GetLength(0); i++)
			{
				if (Regex.IsMatch(song, KnownSongs[i, 0], RegexOptions.IgnoreCase))
				{
					text = KnownSongs[i, 1];
					song = KnownSongs[i, 2];
					stringToEscape = KnownSongs[i, 3];
				}
			}
		}
		string text2 = ((kind == "song") ? song : ("the " + song + " " + kind));
		if (app != "youtube" && Match(Apps(), "spotify").Count > 0)
		{
			if (flag)
			{
				text2 = "their Liked Songs";
			}
			string text3 = (flag ? null : (text ?? SpotifyLink(Fetch("https://html.duckduckgo.com/html/?q=" + Uri.EscapeDataString("site:open.spotify.com/" + ((kind == "song") ? "track" : kind) + " " + song)), kind, song)));
			if (kind == "song" && text3 != null)
			{
				string before = NowPlaying();
				Run("spotify:track:" + text3);
				if (!Started(before) && !PressPlay(song))
				{
					return "Opened " + song + " in Spotify, but couldn't press play. Hit the green Play button.";
				}
				return "Playing " + song + " on Spotify.";
			}
			if (flag || text3 != null)
			{
				Run(flag ? "spotify:collection:tracks" : ("spotify:" + kind + ":" + text3));
				if (!PressPlay(flag ? "Liked Songs" : song))
				{
					return "Opened " + text2 + " in Spotify, but couldn't press play. Hit the green Play button.";
				}
				return "Playing " + text2 + " on Spotify.";
			}
			Run("spotify:search:" + Uri.EscapeDataString(song));
			if (!PressPlay(song))
			{
				return "Couldn't find " + text2 + " to play, so Spotify is open on a search for it.";
			}
			return "Playing " + text2 + " on Spotify.";
		}
		string text4 = Searches[0, 1] + Uri.EscapeDataString(stringToEscape);
		Match match2 = Regex.Match((kind == "song") ? "" : (Fetch(text4 + "&sp=EgIQAw%253D%253D") ?? ""), "\"watchEndpoint\":\\{\"videoId\":\"([A-Za-z0-9_-]{11})\",\"playlistId\":\"([A-Za-z0-9_-]+)\"");
		string text5 = (match2.Success ? null : Find(Fetch(text4), "\"videoId\":\"([A-Za-z0-9_-]{11})\""));
		if (!match2.Success && text5 == null)
		{
			Run(text4);
			return "Couldn't find " + text2 + " to play, so YouTube is open on a search for it.";
		}
		Run(match2.Success ? ("https://www.youtube.com/watch?v=" + match2.Groups[1].Value + "&list=" + match2.Groups[2].Value) : ("https://www.youtube.com/watch?v=" + text5));
		return ((app == "spotify") ? "Spotify isn't installed, so playing " : "Playing ") + text2 + " on YouTube.";
	}

	private static bool ClickPlay(string name)
	{
		for (int i = 0; i < 30; i++)
		{
			Thread.Sleep(500);
			foreach (OpenWindow item in Windows())
			{
				if (!string.Equals(item.Program, "Spotify", StringComparison.OrdinalIgnoreCase) || item.Handle == IntPtr.Zero)
				{
					continue;
				}
				AutomationElement automationElement = null;
				int num = 0;
				foreach (AutomationElement item2 in AutomationElement.FromHandle(item.Handle).FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button)))
				{
					int num2 = PlayScore(item2.Current.Name, name);
					if (num2 > num)
					{
						automationElement = item2;
						num = num2;
						if (num == 3)
						{
							break;
						}
					}
				}
				if (!(automationElement == null) && (i >= 6 || num != 1) && automationElement.TryGetCurrentPattern(InvokePattern.Pattern, out var patternObject))
				{
					((InvokePattern)patternObject).Invoke();
					return true;
				}
			}
		}
		return false;
	}

	internal static string SpotifyLink(string page, string kind, string wanted)
	{
		string text = ((kind == "song") ? "track" : kind);
		string[] array = Words(wanted);
		if (array.Length == 0 || array[0].Length == 0)
		{
			return null;
		}
		foreach (Match item in Regex.Matches(page ?? "", "<a\\b([^>]*)>(.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
		{
			Match match2 = Regex.Match(item.Groups[1].Value, "open\\.spotify\\.com(?:/|%2F)" + text + "(?:/|%2F)([A-Za-z0-9]{22})");
			if (!match2.Success)
			{
				continue;
			}
			string[] array2 = Plain(Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(item.Groups[2].Value, "<[^>]+>", "")), "\\S*(spotify\\.com|https?://|www\\.)\\S*", " ", RegexOptions.IgnoreCase)).Split(' ');
			int num = 0;
			string[] array3 = array;
			foreach (string value in array3)
			{
				if (Array.IndexOf(array2, value) < 0)
				{
					num++;
				}
			}
			if (num == 0 || (num == 1 && array.Length >= 3))
			{
				return match2.Groups[1].Value;
			}
		}
		return null;
	}

	internal static int PlayScore(string label, string name)
	{
		Match match = Regex.Match(label ?? "", "^(?:Play|Lire)\\s+(.+)$", RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			return 0;
		}
		string[] array = Words(match.Groups[1].Value);
		string[] array2 = Words(name);
		bool flag = Within(array, array2);
		bool flag2 = Within(array2, array);
		if (!flag || !flag2)
		{
			if (!flag)
			{
				if (!flag2)
				{
					return 0;
				}
				return 1;
			}
			return 2;
		}
		return 3;
	}

	private static string[] Words(string text)
	{
		string text2 = Plain(Regex.Replace(text ?? "", "\\s+(by|par)\\s+.*$", "", RegexOptions.IgnoreCase));
		return ((text2 == "titres likes") ? "liked songs" : text2).Split(' ');
	}

	private static bool Within(string[] part, string[] whole)
	{
		if (part.Length == 0 || part[0].Length == 0)
		{
			return false;
		}
		foreach (string value in part)
		{
			if (Array.IndexOf(whole, value) < 0)
			{
				return false;
			}
		}
		return true;
	}

	private static string SpotifyTitle()
	{
		string result = "";
		foreach (OpenWindow item in Windows())
		{
			if (string.Equals(item.Program, "Spotify", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(item.Title))
			{
				if (TrackTitle(item.Title))
				{
					return item.Title;
				}
				result = item.Title;
			}
		}
		return result;
	}

	private static bool TrackTitle(string title)
	{
		if (Regex.IsMatch(title ?? "", "^\\S.* - \\S"))
		{
			return !Regex.IsMatch(title, "^Spotify(?:$|\\s)| - Spotify(?:$|\\s)", RegexOptions.IgnoreCase);
		}
		return false;
	}

	internal static bool Started(string before, string title)
	{
		if (title != before)
		{
			return TrackTitle(title);
		}
		return false;
	}

	private static bool Started(string before)
	{
		for (int i = 0; i < 24; i++)
		{
			string title = NowPlaying();
			if (Started(before, title))
			{
				return true;
			}
			Thread.Sleep(500);
		}
		return false;
	}

	private static string Find(string page, string pattern)
	{
		Group obj = Regex.Match(page ?? "", pattern).Groups[1];
		if (!obj.Success)
		{
			return null;
		}
		return obj.Value;
	}

	private static string Download(string address)
	{
		ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(address);
		httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";
		httpWebRequest.Headers["Accept-Language"] = "en";
		httpWebRequest.Timeout = 8000;
		httpWebRequest.ReadWriteTimeout = 8000;
		try
		{
			using WebResponse webResponse = httpWebRequest.GetResponse();
			using StreamReader streamReader = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8);
			return streamReader.ReadToEnd();
		}
		catch (WebException)
		{
			return "";
		}
		catch (IOException)
		{
			return "";
		}
	}

	private static string Window(string wanted, string action)
	{
		if (action != "switch_to" && action != "minimize" && action != "close")
		{
			return "There's no window action called " + action + ".";
		}
		string want = Regex.Replace(Plain(wanted), "^the |( app| application| game| tab| window)$", "");
		if (want.Length == 0)
		{
			return "Which app, choom?";
		}
		wanted = wanted.Trim();
		List<OpenWindow> list = Best(want, out var best);
		if (list.Count == 0)
		{
			return Unlisted() ?? (wanted + " isn't open.");
		}
		OpenWindow first = list.Find((OpenWindow window) => Score(Plain(window.Program), want) == best) ?? list[0];
		list = list.FindAll((OpenWindow window) => window.Program == first.Program);
		if (action == "switch_to")
		{
			if (!Steer(list[0], action))
			{
				return "Couldn't bring " + wanted + " to the front.";
			}
			return "Switched to " + wanted + ".";
		}
		if (action == "close" && Array.IndexOf(Browsers, list[0].Program.ToLowerInvariant()) >= 0 && Score(Plain(list[0].Program), want) == 0)
		{
			if (!Steer(list[0], "close_tab"))
			{
				return "Couldn't reach the " + wanted + " tab to close it.";
			}
			return "Closed the " + wanted + " tab.";
		}
		bool flag = true;
		foreach (OpenWindow item in list)
		{
			flag &= Steer(item, action);
		}
		if (action == "minimize")
		{
			return "Minimized " + wanted + ".";
		}
		if (!flag)
		{
			return "Asked " + wanted + " to close, but it's still open. It might be asking you to save.";
		}
		return "Closed " + wanted + ".";
	}

	private static List<OpenWindow> Best(string want, out int best)
	{
		List<OpenWindow> list = new List<OpenWindow>();
		best = 0;
		foreach (OpenWindow item in Windows())
		{
			int num = Math.Max(Score(Plain(item.Program), want), Score(Plain(item.Title), want));
			if (num != 0 && num >= best)
			{
				if (num > best)
				{
					best = num;
					list.Clear();
				}
				list.Add(item);
			}
		}
		return list;
	}

	internal static List<OpenWindow> WindowsNamed(string wanted, int least = 1)
	{
		string want = Regex.Replace(Plain(wanted), "^(?:the|my|your|ur|our|that|this) |( app| application| game| tab| window)$", "");
		int best = 0;
		List<OpenWindow> list = ((want.Length == 0) ? new List<OpenWindow>() : Best(want, out best));
		if (list.Count == 0 || best < least)
		{
			return new List<OpenWindow>();
		}
		OpenWindow first = list.Find((OpenWindow window) => Score(Plain(window.Program), want) == best) ?? list[0];
		return list.FindAll((OpenWindow window) => window.Program == first.Program);
	}

	internal static bool Sittable(string window)
	{
		if (!TaskbarWords.IsMatch(window ?? "") && !MainframeWords.IsMatch(window ?? ""))
		{
			return WindowsNamed(window, 2).Count > 0;
		}
		return true;
	}

	private static List<OpenWindow> ReadWindows()
	{
		List<OpenWindow> found = new List<OpenWindow>();
		Dictionary<int, string> programs = new Dictionary<int, string>();
		int own = Process.GetCurrentProcess().Id;
		Visit visit = (IntPtr window, IntPtr data) =>
		{
			if (!IsWindowVisible(window) || Native.GetWindow(window, 4) != IntPtr.Zero || (Native.GetWindowLong(window, -20) & 0x80) != 0)
			{
				return true;
			}
			if (DwmGetWindowAttribute(window, 14, out var value, 4) == 0 && value != 0)
			{
				return true;
			}
			int windowTextLength = GetWindowTextLength(window);
			if (windowTextLength == 0)
			{
				return true;
			}
			StringBuilder stringBuilder = new StringBuilder(64);
			GetClassName(window, stringBuilder, stringBuilder.Capacity);
			if (Array.IndexOf(Shell, stringBuilder.ToString()) >= 0)
			{
				return true;
			}
			GetWindowThreadProcessId(window, out var process);
			if (process == own)
			{
				return true;
			}
			if (!programs.TryGetValue(process, out var value2))
			{
				try
				{
					using Process process2 = Process.GetProcessById(process);
					value2 = process2.ProcessName;
				}
				catch (ArgumentException)
				{
					value2 = "";
				}
				catch (InvalidOperationException)
				{
					value2 = "";
				}
				programs[process] = value2;
			}
			StringBuilder stringBuilder2 = new StringBuilder(windowTextLength + 1);
			GetWindowText(window, stringBuilder2, stringBuilder2.Capacity);
			found.Add(new OpenWindow(window, stringBuilder2.ToString(), value2));
			return true;
		};
		EnumWindows(visit, IntPtr.Zero);
		GC.KeepAlive(visit);
		return found;
	}

	private static bool Apply(OpenWindow window, string action)
	{
		IntPtr handle = window.Handle;
		if (action == "minimize")
		{
			ShowWindow(handle, 6);
			return true;
		}
		if (action == "close")
		{
			PostMessage(handle, 16, IntPtr.Zero, IntPtr.Zero);
			for (int i = 0; i < 20; i++)
			{
				if (!IsWindow(handle))
				{
					break;
				}
				Thread.Sleep(100);
			}
			return !IsWindow(handle);
		}
		if (!Front(handle))
		{
			return false;
		}
		if (action != "close_tab")
		{
			return true;
		}
		Native.keybd_event(17, 0, 0, UIntPtr.Zero);
		Native.keybd_event(87, 0, 0, UIntPtr.Zero);
		Native.keybd_event(87, 0, 2, UIntPtr.Zero);
		Native.keybd_event(17, 0, 2, UIntPtr.Zero);
		return true;
	}

	internal static bool Front(IntPtr handle)
	{
		if (IsIconic(handle))
		{
			ShowWindow(handle, 9);
		}
		int currentThreadId = GetCurrentThreadId();
		int windowThreadProcessId = GetWindowThreadProcessId(Native.GetForegroundWindow(), out var _);
		bool flag = windowThreadProcessId != 0 && windowThreadProcessId != currentThreadId && AttachThreadInput(currentThreadId, windowThreadProcessId, attach: true);
		BringWindowToTop(handle);
		SetForegroundWindow(handle);
		if (flag)
		{
			AttachThreadInput(currentThreadId, windowThreadProcessId, attach: false);
		}
		for (int i = 0; i < 10; i++)
		{
			if (!(Native.GetForegroundWindow() != handle))
			{
				break;
			}
			Thread.Sleep(50);
		}
		return Native.GetForegroundWindow() == handle;
	}
}
