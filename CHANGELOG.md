# Changelog

## Atalhos por voz
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
