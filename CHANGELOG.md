# Changelog

> A partir daqui, cada alteração é uma nova versão do mod (mostrada no Mainframe como "Modded vX.Y.Z").

## v0.0.12 — Transmissão de Voz

- O botão **Settings** do Mainframe voltou (o texto da versão estava por cima dele).
- Nova seção **BROADCAST** nas configurações: a voz da Capri também toca dentro de um dispositivo que você escolhe, como um cabo de áudio virtual (VB-Audio Virtual Cable). Assim ela entra no "microfone" do Discord, OBS, gravadores etc.
- Dá para escolher em qual dispositivo ela fala ("Into: ..."), e o "Send my mic too" manda a sua voz junto, misturada com a dela.
- Por voz: "Capri, transmite sua voz no meu microfone" liga, e "Capri, para de transmitir" desliga. Fica ligado até você pedir para desligar.

## v0.0.11 — Versão de volta no título

- O canto do Mainframe volta a mostrar "Modded v0.0.11" inteiro (o texto não cabia e cortava a versão).

## v0.0.10 — Sem travadas no Windows

- A rodinha "presa" no Mainframe usava um gancho de mouse ligado o tempo todo, o que fazia o mouse do Windows inteiro engasgar quando a Capri estava ocupada. Agora o gancho só liga enquanto o cursor está em cima do Mainframe e ele está na frente.
- O changelog redesenha menos vezes (só quando algo se mexe), gastando menos CPU.

## v0.0.9 — Resize e rodinha consertados, changelog novo

- **Redimensionar de verdade**: a janela do Mainframe não tem moldura, então o Windows ignorava as bordas. Agora o próprio Mainframe cuida disso. Ao puxar qualquer borda ou canto, aparece uma **prévia neon animada** com o tamanho final (ex.: "90% · 700 tall"), com um estalo a cada 5% e um som ao soltar.
- **Rodinha do mouse**: com o Mainframe na frente, a rodinha em cima dele rola só o Mainframe. Não depende mais do evento de foco que falhava.
- **Changelog novo**: janela própria em estilo neon, com cartões por versão que deslizam um após o outro, a versão mais nova em destaque piscando, rolagem suave (rodinha, setas, Page Up/Down), destaque ao passar o mouse, sons ao abrir, passar e fechar. Fecha no X ou com Esc e pode ser arrastada pelo topo.

## v0.0.8 — Redimensionar por qualquer lado

- Nova opção "90%" em Mainframe size.
- O Mainframe agora redimensiona por qualquer borda ou canto. Os lados esquerdo e direito mudam o tamanho de tudo (de 5 em 5%, entre 75% e 200%). Nas configurações, o topo e a base mudam a altura da página. Os cantos fazem as duas coisas.
- Com o Mainframe focado, a rodinha do mouse em cima dele rola só o Mainframe e não mexe mais no programa que está por baixo.

## v0.0.7 — Mainframe em 75%

- Nova opção "75%" em Settings > Mainframe size, para o Mainframe caber em telas menores.

## v0.0.6 — Mainframe redimensionável

- Nas configurações, dá para puxar a borda de baixo do Mainframe para deixar a janela mais baixa (ou mais alta). O resto vira rolagem, e o tamanho fica salvo.

## v0.0.5 — Ajudantes, estatísticas e proteção do mod

- **Bloquear atualizações oficiais** (ligado por padrão): o app não baixa mais a versão oficial por cima do mod.
- **Perfis**: botões Calm (sem olhos, sem ronronar, sem alertas), Normal e Chaos (tudo no máximo).
- **Testar agora**: botões Zap (raio), Pet (carinho) e Time (fala a hora).
- **Estatísticas**: carinhos, ronronadas, arremessos, dias juntos e lembretes pendentes.
- **Lembretes por voz**: "Capri, me lembra de beber água em 30 minutos" (ou "daqui a 10 minutos me lembra de..."). Aceita segundos, minutos e horas. Os lembretes ficam na memória e somem se a Capri for fechada.
- **Pomodoro**: 25/5 ou 50/10 minutos; ela avisa o fim do foco e da pausa com raios.
- **Fala a hora**: de hora em hora ou a cada meia hora (não fala se estiver escondida ou quieta).
- **Alertas do PC**: avisa quando a CPU fica acima de 90% por 30 segundos (no máximo a cada 15 min) e quando a bateria cai para 20% fora da tomada.

## v0.0.4 — Novas opções no Mainframe

- O botão Settings ficou mais estreito e mais à esquerda, e o texto da versão não cobre mais ele.
- Nova seção MOD nas configurações:
  - Botão "Changelog", que mostra esta lista de mudanças dentro da Capri.
  - "Voice shortcuts": liga/desliga os atalhos por voz (deita, quieta, se esconde, aparece, sobe no mouse).
  - "Petting": liga/desliga o carinho com o mouse.
  - "Purr chance": quanto ela ronrona no carinho (nunca, às vezes, frequente, sempre).
  - "Pet sensitivity": quão fácil o carinho é reconhecido (suave, normal, fácil).

## v0.0.3 — Versão não cobre mais o botão

- O texto da versão no Mainframe começava em cima do botão quadrado ao lado do título. Agora começa mais à direita e não cobre mais o botão.

## v0.0.2 — Versão aparece inteira
- Corrigido: o Mainframe mostrava só "Modded"; agora mostra o número da versão também.

## v0.0.1 — Versão do mod no Mainframe
- O Mainframe mostra "Modded v0.0.1" no lugar da versão original.

## Correção do nome
- Quando o reconhecimento de voz escreve o nome dela errado ("capre", "cápria", "cabre", "kapri"...), o texto é corrigido para "Capri".

## Atalhos por voz
- Corrigido: as falas das ordens saem na hora, sem atraso e sem sumir.
- Corrigido: as falas das ordens agora aparecem. Na conversa por voz e fora dela, a Capri cria a própria fala sobre o que acabou de fazer.
- Na conversa por voz, a Capri responde às ordens com falas próprias geradas pela IA (seguindo o que ela fez). O balão de fala some quando ela se esconde.
- Corrigido: a Capri agora volta quando você diz "aparece" depois de mandar ela se esconder por voz.
- Cada ordem tem raios/som e uma fala dela (falas variadas). Nova ordem: "desce do mouse".
- Fale com a Capri: "deita", "fica quieta", "se esconde", "aparece" e "sobe no meu mouse" (este último com raios, som e uma fala). Funciona mesmo se o reconhecimento de voz escrever errado ("capre", "kieta", "mause").

## Raios sólidos com glitch
- Raios em cor sólida, vermelho e verde da Capri, sem brilho ou degradê.
- Glitch: cópia verde deslocada, trechos pulando de lado, piscadas e linhas de scanline.

## Raios vermelhos
- Os raios da Capri agora são vermelhos, finos e angulares como no anime, no lugar dos raios rosa/laranja que pareciam trovões.

## 2026-10-31 — Raios no estilo do anime
- Teleporte e pulos: o feixe reto virou um raio em zigue-zague (rosa/laranja com núcleo branco) com ramificações que mudam a cada quadro.
- Transformação em raio: no começo e no fim ela fica disforme, esticando para cima, afinando e ondulando como chama.
- Faíscas: arcos elétricos, espinhos laranja saindo do chão e blocos de glitch rosa/ciano ao chegar, sair, ser agarrada, arremessada, quicar nas bordas e cair com força; rastro de faíscas no voo e ao balançar rápido; estalinhos nos pés ao andar.
- Sons novos: estalo de raio (trovão curto) ao sumir/aparecer, crepitar elétrico no arremesso, na queda e no pulo, e faísca baixinha ao agarrar.

## 2026-10-31 — Física ao arrastar
- Ao segurar a Capri, ela balança como pêndulo: pende para o lado contrário do movimento, passa um pouco do ponto e volta ao centro quando o mouse para.
- Soltar com o mouse em movimento rápido arremessa ela: voa com a velocidade do mouse (inclusive para cima), gira com o voo, quica nas bordas da tela e cai num lugar para sentar.

## 2026-10-31 — Botão TYPE/TALK ao lado dela
- O botão TYPE/TALK agora aparece ao lado da Capri (à direita, ou à esquerda perto da borda da tela), e não mais em cima do sprite.

## 2026-10-31 — Carinho menos sensível
- Só conta como carinho um vai-e-vem de verdade: cada ida precisa andar uns 30 px e as viradas precisam vir em ritmo (até 0,7 s entre elas). São 6 viradas para ela reagir (antes eram 4, com qualquer tremidinha de 6 px).
- A faísca no cursor só aparece depois que o carinho começou de fato, não ao passar o mouse por cima.

## 2026-10-31 — Rework do carinho
- Ao esfregar o cursor nela: faísca com coraçãozinho de pixel, anel verde e um "blip" digital a cada vai-e-vem.
- Reação: anéis verde/azul, corações de pixel que pulam, sobem balançando e dão glitch, com acorde digital de três notas e o texto "+AFFECTION".
- Ronronar digital (1 em cada 3 carinhos): som de ronronar sintetizado, onda de osciloscópio "~prrrr.exe~" e uma fala própria de ronronar.
- Fim: os corações se juntam num coração grande que estoura em pixels com anel, e um som de "fechamento" em duas notas.

## 2026-10-31 — Olhos: caixinha irregular e opções no Mainframe
- Caixinha com formatos mais irregulares (incluindo "estilhaço" de 5–6 cantos) e cantos que balançam devagar.
- Nova seção EYES no Mainframe: liga/desliga, frequência (Rarely/Sometimes/Often/Swarm), tamanho (Small/Medium/Large/Mixed), máximo de olhos ao mesmo tempo (1–5) e botão "Peek now".

## 2026-10-31 — Olho no estilo da Capri
- Visual igual à arte dela: caixinha verde (visor) atrás do olho, olho preto com espinhos, anel verde, íris azul e pupila branca, em cores chapadas.
- A caixinha varia de forma: torta, trapézio largo em cima, trapézio largo embaixo ou paralelogramo inclinado.
- Piscada fechada desenhada como o "wink" dela (traço preto curvo com espinho cruzado).

## 2026-10-31 — Rework do EyePopup
- Os olhos da Capri aparecem com mais frequência (a cada 25–75 s, às vezes 2–3 de uma vez), em lugares aleatórios da tela.
- Cada olho é diferente: semicerrado, sonolento, arregalado ou normal, com tamanho, inclinação e cores neon variados.
- Animação cyberpunk: linha neon piscando que se abre em olho com glitch e aberração cromática; ao sumir, as pálpebras fecham e a linha colapsa num ponto.
- O olho segue o cursor, pisca e tem HUD com texto digitando.
- Sons sintetizados de boot e shutdown digital.
- Sempre por cima, sem fundo, os cliques passam por ele e ele nunca rouba o foco do teclado nem do mouse.

## 2026-10-31
- Repositório criado com o código decompilado e recompilável (base 0.7.7).
- Projeto movido para `D:\usuario alternativo\Documents\Capri Desktop` (capri-decomp, capri-test, capri-normal).

## Antes do repositório
- Tradução da Capri ajustada.
- EyePopup adicionado.
