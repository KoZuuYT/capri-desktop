# LOGS — Capri Desktop (mod pessoal)

> Leia este arquivo antes de qualquer coisa em uma sessão nova.

## O que é
Código decompilado (C#, .NET 8 WinForms/WPF) do programa que traz a **Capri** (Cyberpunk Edgerunners 2) para o desktop. Os termos do programa permitem modificações para **uso pessoal**, por isso o repositório é **privado** e nunca deve ser público nem redistribuído.

- Versão base do app: **0.7.7** (`src/Properties/AssemblyInfo.cs`)
- Pasta geral: `D:\usuario alternativo\Documents\Capri Desktop\`
  - `capri-decomp\` — fonte + git (`src\`)
  - `capri-test\` — build modificado que roda (`Capri.exe.bak` = executável original)
  - `capri-normal\` — cópia do `Capri.exe`/`Uninstall.exe` originais 0.7.7
- Instalação real e dados (memórias, chat, modelos, ~6 GB): `C:\Users\Usuário\AppData\Local\Capri` — **não mover**: o app procura os dados em `%LOCALAPPDATA%\Capri` (fixo no código, `Program.cs:48`)

## Fluxo de trabalho
1. Editar em `src/CapriDesktop/*.cs`
2. `dotnet build src/Capri.exe.csproj -c Release -o src/out` (o normal são milhares de avisos CA1416 e **0 erros**). **Sempre com `-o src/out`**: sem isso a saída vai para `src/bin/Debug/...` e o capri-test continua com o executável velho
3. Copiar `src/out/*` para `capri-test\` e testar
4. Subir a versão do mod (`ModVersion` em `Mainframe.cs`, +1 no último número a cada alteração) e criar a seção `## vX.Y.Z` no `CHANGELOG.md`. Atualizar e a seção "Onde paramos" abaixo; commit + push

## Mapa dos sistemas (`src/CapriDesktop/`, 156 arquivos)
- **Núcleo/UI:** `Companion.cs` (~15 mil linhas, quase tudo passa por ele), `Program`, `Overlay`, `SpeechBox`, `TalkPill`, `TypeBox`, `TaskPanel`, `Notice`, `PetHearts`
- **IA:** `Brain`, `Agent*`, `ClaudeCode*`, `Claude*`, `Completion*`/`OpenAiRequest`, `Cloud*`, modelos locais (`Model*`), ferramentas (`ToolCall`, `AgentTool*`)
- **Voz/audição:** `Voice*`, `QwenVoice`, `LocalVoice`, `VoiceFx`, `Ears`, `Hearing`, `LocalEars`, `Dictation`, `WakeWord`, `Mic*`, `ISound*`, `SoundTap`
- **Música/dança:** `BeatLock`, `Tempo`, `Onset`, `Groove`, `Biquad`, `Tune*`, `MiniPlayer`
- **Desktop:** `Ledge*`, `Shelf`, `Sight`, `Glances`, `OpenWindow`, `Hands`, `HerCursor`, `HerPointer`, `Takeover`, `Computer`, efeitos (`Shatter`, `Tear`, `Neon`)
- **Outros:** Discord RPC (`Discord*`, `Presence`), `Updater`/`Release*`, `Startup`, `Preferences`, `SavedChat`, `Meeting`, `Note`, `Job*`
- **Recursos:** `src/Capri.Sprite.*.png` (poses idle/walk/dance/perch/laydown...), `src/Capri.Voice.*.wav`

## Onde paramos
- Projeto decompilado e recompilando sem erros (19 builds feitos). Repositório criado em 2026-10-31.
- Feito antes do repo: **tradução da Capri ajustada** e **EyePopup adicionado** (`EyePopup.cs` + `src/EyePopup.png`).
- 2026-10-31: projeto movido de `AppData\Local\Temp` para `D:\usuario alternativo\Documents\Capri Desktop`.
- 2026-10-31: **rework do EyePopup** (`EyePopup.cs`, `CheckEyePopup` em `Companion.cs`, cues `EyeOpen`/`EyeClose` em `Cue.cs`/`Sfx.cs`). O olho agora é desenhado por código (o `EyePopup.png` não é mais usado), em 4 humores (semicerrado, sonolento, arregalado, normal) com tamanho, inclinação e paleta aleatórios. Animação de boot e shutdown com glitch, olhar que segue o cursor e piscadas. A janela é transparente pixel a pixel, os cliques passam por ela e ela nunca pega foco. Aparece a cada 25–75 s; em 1 de cada 5 vezes vem um grupo de 2–3 olhos. Testado: o olho aparece corretamente na tela.
- 2026-10-31: olho redesenhado no estilo da arte da Capri (visor verde com 4 formas, olho preto com espinhos, anel verde, íris azul, pupila branca). Animações e sons mantidos.
- 2026-10-31: caixinha irregular e balançando; seção EYES no Mainframe (`Mainframe.cs`, coluna direita y=790; página aumentada de 860 para 960). Preferências `EyePace`, `EyeSize`, `EyeMost` em `Preferences.cs`.
- 2026-10-31: rework do carinho (`PetHearts.cs` virou janela layered com tipos Stroke/React; cues `PetStroke`/`PetHappy`/`PetPurr`/`PetEnd` em `Cue.cs`/`Sfx.cs`; `PurrLines` e `petPurr` em `Companion.cs`, perto de `Stroke`/`Petted`). Ronrona em 35% dos carinhos.
- 2026-10-31: detecção do carinho menos sensível (`petSwing` em `Stroke`: ida >= 30 px, viradas em até 700 ms, 6 viradas).
- 2026-10-31: TalkPill posicionado ao lado do sprite (método que chama `pill.Place` em `Companion.cs`).
- 2026-10-31: física do arrasto estilo shimeji (`Swing`/`Throw`/`Fly` em `Companion.cs`, perto de `Land`; usa `leanAngle` para o balanço e `flyVX` + `fallSpeed` no `Tumble` para o arremesso).
- 2026-10-31: raios estilo anime (`Crackle.cs` = overlay de faíscas: arcos, espinhos, blocos; `Shatter.Beam` usa `Crackle.Bolt`; `Shatter.Sliced` com `warp` deforma o corpo na transformação; `Zap`/`ZapTrail` em `Companion.cs` perto de `Swing`; cues `Zap`/`Spark` e `Strike`/`Crackle` em `Sfx.cs`).
- 2026-10-31: raios trocados para o vermelho do anime (linhas finas e angulares em 45°, sem núcleo branco; `Crackle.Bolt` e cores `Hot`/`Ember`/`Flame`/`Ice` em `Crackle.cs`).
- 2026-10-31: raios em cor sólida (vermelho `Hot` 247,6,63 + verde `Flame` 0,217,93, tons exatos da Capri; sem antialias nem degradê) e efeitos de glitch: cópia verde deslocada, pedaço do raio pulando de lado, piscadas e linhas de scanline (`Crackle.cs`).

- 2026-10-31: atalhos por voz (`VoiceShortcut` e `RideByVoice` em `Companion.cs`, chamados no começo de `ChatHeard`): deita, fica quieta, se esconde, aparece, sobe no mouse (com raios, som e fala). Frases com "não" são ignoradas. O nome "Capri" não é exigido e os verbos aceitam erros comuns do reconhecimento (capre, kieta, aparesse, mause...).
- 2026-10-31: "aparece" depois de "se esconde" por voz: ao se esconder por voz durante a conversa contínua ela não desliga mais o microfone (`hideKeepsEars` em `ShowHidden`), então ouve a ordem de voltar; enquanto escondida ignora outras falas.
- 2026-10-31: balão some junto quando ela se esconde (`box.Hide()` em `ShowHidden`, `|| hidden` em `UpdateTalk`). Falas das ordens por voz: na conversa contínua `SayOne(what, ...)` manda um `agent.Tell` descrevendo o que ela fez e a IA responde com as próprias palavras; fora da conversa usa as frases fixas.
- 2026-10-31: falas das ordens corrigidas: `agent.Tell` (contextual_update) não faz a IA responder; agora `SayOne` usa `agent.Say` (user_message com nota de sistema) e renova `spoke`/`stirred` para a resposta não ser descartada. Fora da conversa gera a fala com `brain.Ask` (frase fixa só se o cérebro falhar/estiver offline).
- 2026-10-31: falas das ordens sem atraso: na conversa `SayOne` volta a usar `agent.Tell` (enviado na hora, a IA responde junto com a resposta à fala do usuário; o `agent.Say` extra criava um segundo turno que chegava tarde ou se perdia). Fora da conversa mostra na hora uma fala que o cérebro já escreveu antes (`orderLines`, até 3 por ordem) e escreve a próxima em segundo plano; a frase fixa só aparece na primeira vez ou sem cérebro.
- 2026-10-31: correção do nome na voz (`FixName`/`NameSlip` em `Companion.cs`, aplicada em `ChatHeard` e `Hear`): erros do reconhecimento como capre, cápria, cabre, kapri viram "Capri" antes de qualquer outra coisa (atalhos e IA). "caprese" e palavras comuns como cabra, capa, sobre não são trocadas.
- 2026-10-31: **v0.0.1** — Mainframe mostra "Modded v" + `ModVersion` (`Mainframe.cs`). Daqui pra frente toda alteração sobe a versão.
- 2026-10-31: **v0.0.2** — rótulo `build` alargado (x=160, largura 132; também em `build.Left`), antes cortava o número.
- 2026-10-31: **v0.0.3** — rótulo `build` movido para x=185, largura 107 (cobria o botão ao lado do título).
- 2026-10-31: **v0.0.4** — Settings em x=120 largura 72, versão em x=196 largura 98. Seção MOD na coluna esquerda da página (y=822): botão Changelog (CHANGELOG.md embutido como `Capri.Changelog.md`, `ShowChangelog`), switches `VoiceOrders`/`Petting` e sliders `PurrChance`/`PetFeel` (`Preferences.cs`; `PetFeels`/`PurrChances` em `Companion.cs`).
- 2026-10-31: **v0.0.5** — página do Mainframe de 960 para 1160. Esquerda: `Block official updates` (y=958, `BlockUpdates`, checado em `CheckForUpdates` e na checagem automática), Profile Calm/Normal/Chaos (`UseProfile`), Test now Zap/Pet/Time, seção STATS (y=1050, `Pets`/`Purrs`/`Throws`/`FirstMet`). Direita: seção HELPERS (y=958) com Pomodoro, Say the time (`Chime`) e PC alerts (`GetSystemTimes` + `PowerStatus`). Lembretes por voz em `ReminderByVoice` (chamado no começo de `VoiceShortcut`, só em memória). Tudo roda em `CheckHelpers` no timer `signals`.
- 2026-10-31: **v0.0.6** — página de settings redimensionável pela borda de baixo (hit-test 15 no `WndProc` do Mainframe, `OnResizeEnd` salva `MainframeTall`, usado em `Fit`; 0 = automático).
- 2026-10-31: **v0.0.7** — opção 75% em `FrameSizes`/`FrameSizeNames` (Mainframe.cs) e limite mínimo de `SetMainframeSize` baixado de 100 para 75.
- 2026-10-31: **v0.0.8** — opção 90%. Hit-test de todas as bordas/cantos no `WndProc` do Mainframe. Em `OnResizeEnd`, a largura vira zoom (`SetMainframeSize`, passos de 5%) e a altura vira `MainframeTall`. `ShowFrameSize` mostra tamanhos fora da lista (ex.: 85%). Hook de mouse de baixo nível (`CatchWheel`, WH_MOUSE_LL), ativo só enquanto o Mainframe está focado: engole a rodinha sobre ele e repassa por `PostMessage` ao controle debaixo do cursor.
- 2026-10-31: **v0.0.9** — sem `WS_THICKFRAME` o hit-test de borda não redimensiona; agora `WM_NCLBUTTONDOWN` (161) nas bordas chama `StartResize`, um timer (`ResizeStep`) segue o cursor com a prévia `SizeGhost` e `FinishResize` aplica o zoom e a altura. O hook da rodinha fica instalado enquanto visível (`OnVisibleChanged`), exige `GetForegroundWindow() == Handle` e chama `OnMouseWheel` direto. Changelog em `ChangelogView.cs`. Teste na tela não foi possível: a Capri não fica em primeiro plano para a ferramenta de clique.
- 2026-10-31: **v0.0.10** — o hook WH_MOUSE_LL sempre ligado travava o mouse do Windows. Agora `WatchWheel` (timer de 120 ms) só instala o hook com o cursor sobre o Mainframe em primeiro plano, e o callback devolve na hora tudo que não é rodinha. `ChangelogView` com timer de 30 ms e redesenho só quando anima.

## Pedidos pendentes
- (preencher)

## Armadilhas técnicas
- Aviso MSB3245 sobre "System.Core" no build é inofensivo.
- Captura de tela comum (`CopyFromScreen`) não mostra janelas layered como o EyePopup; use BitBlt com `CAPTUREBLT` (0x40000000) para verificar.
- O `Updater` pode baixar a versão oficial por cima do mod. Desde a v0.0.5 isso fica bloqueado pela preferência `BlockUpdates`, que vem ligada.
