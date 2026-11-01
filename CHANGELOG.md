# Changelog

> A partir daqui, cada alteração é uma nova versão do mod (mostrada no Mainframe como "Modded vX.Y.Z").

## v0.0.27 — Correções no Pega o olho e sons no tempo certo

- **Pega o olho:** clicar fora de um olho fazia a camada escura do jogo passar na frente dos olhos, deixando a tela mais escura e os olhos impossíveis de clicar. Agora os olhos, o placar e os avisos voltam pra frente a cada quadro, e o fundo da arena ficou mais claro.
- **Sons no tempo certo:** os efeitos sonoros tocavam um depois do outro numa fila, então no minijogo e no enxame de olhos da travessura eles atrasavam e saíam fora de sincronia. Agora cada som começa na hora e se mistura com os que ainda estão tocando (mixer ao vivo, atraso de ~60 ms).

## v0.0.26 — Pacote de diversão

- **Dar arquivos pra ela comer:** arraste qualquer arquivo ou pasta até a Capri. Aparece um anel neon com "nhac?", o ícone do arquivo voa girando até a boca dela, vira farelo de pixel com um "NHAC!" e um som de mordida, e ela comenta o "sabor" (foto, texto, música, programa, zip...). **Nada é apagado nem movido:** só o ícone é usado na animação. Os primeiros lanches de cada 10 minutos aumentam o carinho.
- **Minijogo "Pega o olho":** a tela escurece numa arena neon, contagem 3-2-1-JÁ, e por 30 segundos os olhos dela abrem pela tela. Olho verde vale 1, olho vermelho (mais rápido) vale 3, e 5 acertos seguidos aumentam o combo (até x3). Placar com tempo, pontos, combo e recorde, explosão de pixels a cada acerto e contagem dos pontos no final, com "NOVO RECORDE!".
- **Humor do dia:** todo dia ela acorda Sonolenta, Elétrica, Carente ou Rabugenta. Ela avisa com um cartão e uma fala, e o humor muda o jeito dela conversar e agir (boceja, solta faísca, pede carinho ou resmunga; carinho vale mais quando ela está carente e menos quando está rabugenta; mais travessuras quando está elétrica).
- **Sono à noite:** depois da meia-noite, se você largar o PC por 2 minutos (1 minuto se ela estiver sonolenta), ela boceja e dorme, com "Z"s de pixel subindo. Um clique acorda ela (e ela resmunga). Se você ficar acordado até tarde, uma vez por noite ela manda você dormir.
- **Conquistas:** 18 conquistas (carinhos, ronronadas, arremessos, dias juntos, carinho, lanches, recordes, esconde-esconde, travessuras, ficar acordado às 3 da manhã, ver todos os humores). Ao desbloquear aparece um cartão dourado com fanfarra e corações. Janela própria com troféus de pixel e barra de progresso de cada uma.
- **Esconde-esconde:** ela pede pra você contar, some e se esconde atrás de uma borda da tela, só com o topo da cabeça aparecendo. De vez em quando ela espia, e quando o cursor chega perto ela se abaixa e dá uma risadinha. Ache e clique nela! Depois de 25 s aparece uma dica, e se você não achar em 75 s ela ganha.
- **Entrada e saída em pixels:** quando ela aparece ou some, o corpo dela se desfaz (ou se monta) em blocos de pixel cada vez maiores, com scanlines e uma linha brilhante passando por ela; os estilhaços viraram quadradinhos de pixel.
- **Balão de fala estilo terminal:** o balão ganhou uma barra de título "capri@k.mod:~$ tty0" com status ao vivo (RX, PROC... enquanto pensa, TX piscando enquanto fala) e de vez em quando dá um glitch de sinal enquanto ela fala.
- **Avisos neon iguais pra tudo:** lembretes, Pomodoro, CPU, bateria, conquistas, jogos, humor e modo noite agora usam o mesmo cartão neon no canto da tela: entra com glitch, monta um ícone de pixel, digita o título, tem barra de tempo em blocos e fecha como TV antiga. Passar o mouse segura o aviso; um clique fecha.
- **Boot do Mainframe:** ao abrir o Mainframe, ele liga como um monitor antigo, um "K.MOD BIOS" digita as verificações (com seu carinho e o humor do dia), uma barra enche, e a tela se desfaz em blocos revelando o painel. Com sons de boot. Dá pra desligar.
- **No Mainframe:** seções novas **FUN** (humor do dia, botões dos jogos, sono à noite, animação de boot, recorde e lanches) e **ACHIEVEMENTS** (contador e botão para ver todas). No menu do botão direito: submenu **Diversão** com os jogos, as conquistas e o humor do dia.

## v0.0.25 — Instalador K.mod cyberpunk

- **Menu com o nome do mod:** o topo do menu do botão direito agora mostra "CAPRI / K.mod" em vez de "CAPRI / 0.7.7".
- **Instalador novo (`Capri-Mod-Setup-0.0.25.exe`)** com a cara do Mainframe: janela neon sem borda, ícone com a cabeça da Capri, fundo com estrelas e chão neon andando, scanlines, glitches, e liga/desliga como um monitor antigo.
- **5 etapas:** BOOT (terminal que digita sozinho), MODS (lista do que mudou em relação à Capri original, com filtros NOVO/MELHOR/CORRIGIDO), CONFIG, INSTALL (porcentagem grande, barra em blocos, log e fluxo de dados) e ONLINE (resumo com faíscas).
- **Sons:** bipes ao passar o mouse, cliques, interruptores, digitação, acorde ao terminar e alarme se der erro. Dá para desligar no botão "SOM".
- **Opções detalhadas:** atalho na Área de Trabalho, atalho no Menu Iniciar, ligar junto com o Windows, abrir no final; e as configurações iniciais dela (idioma, tamanho, volume dos sons, modo travessura, olhos, carinho, atalhos por voz, sempre por cima). Passando o mouse em cada opção aparece o que ela faz.
- Se ela já estiver instalada, as configurações e memórias atuais são mantidas, a não ser que você ligue "Trocar minhas configurações" (aí é guardada uma cópia de segurança).

## v0.0.24 — Enxame de olhos sincronizado e com olhos vermelhos

- **Sons no tempo certo:** na travessura "Enxame de olhos", os sons não ficam mais numa fila, um esperando o outro acabar. Agora cada olho faz o som exatamente quando abre e quando fecha.
- **Olhos vermelhos:** o enxame sempre traz de 3 a 5 olhos vermelhos misturados aos verdes.

## v0.0.23 — Menu de travessuras em branco e carinho mais limpo

- Os textos do menu **Travessuras** (e da lista "Escolher travessura") estavam aparecendo em preto. Agora ficam brancos como o resto do menu.
- O medidor de carinho no menu ficou mais limpo: sai a barrinha "#----" e fica só "Carinho: 5%" seguido do nível (ex.: "Desconfiada") um pouco menor, em rosa.

## v0.0.22 — Travessuras turbinadas

- **Mais efeitos e sons:** cada travessura agora tem um som próprio (estalo de tela quebrando, "vush", sirene, mola, tremor, lata de spray) e a Capri reage a todas: aparecem chifrinhos neon na cabeça dela, um "hehe" pulando e faíscas, com uma risadinha.
- **Travessuras antigas melhoradas:** o "bug" de tela ficou mais forte, com cores separadas e uma mensagem de erro piscando; o cursor roubado deixa um rastro neon com um coraçãozinho; o aviso de hacker liga e desliga como um monitor antigo, digita a mensagem sozinho e mostra uma barra de "copiando seus memes".
- **Travessuras novas:**
  - **Enxame de olhos:** de 8 a 12 olhos aparecem de uma vez, passando do limite normal.
  - **Puxar uma janela:** ela laça a janela da frente com uma corda neon, puxa pra perto dela e solta, e a janela volta pro lugar.
  - **Sacudir uma janela:** a janela da frente treme como num terremoto e volta pro lugar.
  - **Virar a tela:** a tela gira de cabeça pra baixo como uma carta e depois desvira (um clique desvira antes).
  - **Pichar a tela:** ela picha "CAPRI ESTEVE AQUI" com tinta escorrendo; some sozinho ou com um clique.
- Novo submenu **Travessuras → Escolher travessura** para disparar qualquer uma na hora.
- Continua tudo inofensivo: nada é clicado, digitado ou apagado, as janelas sempre voltam pro lugar, e se você pegar o mouse ou a janela ela solta na hora. Janelas maximizadas ou em tela cheia não são mexidas.

## v0.0.21 — Saudação na sua voz, medidor de carinho e modo travessura

- **Saudação ao abrir:** a Capri não toca mais a gravação fixa em inglês quando liga. Agora ela fala o "Bom dia, choom." (e as outras saudações) em português, com a voz que você escolheu.
- **Medidor de carinho:** ela vai gostando mais de você conforme vocês passam tempo juntos (um pouco a cada minuto com você por perto, mais quando conversam, e quando você faz carinho nela). São 5 níveis: Desconfiada, Curiosa, Amiga, Apegada e Inseparável. Ao subir de nível ela solta corações e comenta. Se você sumir por vários dias, ela esfria um pouquinho. O jeito dela falar muda conforme o nível, e você vê o medidor no menu do botão direito.
- **Modo travessura** (desligado por padrão; ligue no menu do botão direito → Travessuras): de vez em quando ela apronta uma de três: faz a tela "bugar" por um instante, rouba seu cursor e dá uma voltinha com ele, ou abre um aviso falso de "VOCÊ FOI HACKEADO". Tudo inofensivo: ela não clica nem mexe em nada, não faz isso em jogos/tela cheia, e se você puxar o mouse de volta ela solta na hora. Tem também "Fazer uma travessura agora" para testar.

## v0.0.20 — "corp" em vez de "corpo"

- Quando fala português, a Capri agora usa a gíria "corp" (ex.: "Típico corp.") em vez de "corpo", que em português quer dizer outra coisa.

## v0.0.19 — Capri no Linux (Fedora) pelo Wine

- Novo instalador para Linux (`Capri-Mod-Linux-0.0.19.sh`): ele instala o Wine e o que mais faltar, prepara tudo, coloca a Capri no menu de aplicativos e faz ela abrir junto com o computador.
- No Linux, ela ouve os amigos da call pelo som do sistema (o mesmo lugar onde o Discord toca).
- No Linux, ela mesma abre os arquivos que baixa, e as partes que só existem no Windows (controle de música do Windows, reconhecimento de fala do Windows) ficam desligadas em vez de travar.
- Para remover no Linux: `bash ~/.local/share/capri-mod/uninstall.sh` (dá para escolher se apaga também as memórias e os modelos).

## v0.0.18 — Instalador para usar a Capri modificada em outro computador

- Agora existe um instalador (`Capri-Mod-Setup-0.0.18.exe`) que leva a Capri modificada para outro PC, sem precisar instalar o .NET lá.
- Ele instala no mesmo lugar da Capri oficial, cria atalhos no Menu Iniciar (e na Área de Trabalho, se quiser) e aparece em Configurações > Aplicativos para desinstalar.
- Ao desinstalar, dá para escolher se apaga também as memórias, configurações e modelos baixados.

## v0.0.17 — Capri ouve os amigos com mais clareza e entende quando é chamada

- O som da call agora passa por um filtro de verdade antes de virar texto (antes ele era reduzido de um jeito grosseiro que embaralhava as palavras), com volume ajustado e uma pausa de silêncio nas pontas para ela não perder a primeira e a última palavra.
- Ela reconhece mais jeitos de chamar o nome dela ("Cap", "Capre", "Cabre", "Captain"...).
- Quando alguém diz só "Capri" e faz a pergunta logo depois, ela espera um pouquinho e junta as duas partes.
- Depois de ser chamada, ela continua atenta ao amigo por uns 15 segundos, mesmo que ele não repita o nome.

## v0.0.16 — Capri entende os amigos mesmo sem o seu modo de voz ligado

- A transcrição da call usava os ouvidos dela só enquanto o seu chat de voz estava ativo. Fora disso caía no reconhecedor do Windows, que dava erro, e a fala dos amigos sumia. Agora ela usa os próprios ouvidos sempre que estão instalados.

## v0.0.15 — Diário da escuta dos amigos

- A Capri agora anota no `errors.txt` o que acontece com o som da call: o volume que chega, cada fala enviada para transcrição, quantas palavras ela entendeu, e por que uma fala ficou sem resposta. Serve para achar onde a fala dos amigos se perde.

## v0.0.14 — Capri ouve melhor os amigos

- Ela reconhece o nome dela mesmo quando a call distorce ("Cabre", "Kapri", "Cabri", "Capre"...).
- A fala dos amigos não é mais descartada quando o seu microfone capta som: ela ouve e espera a sua vez para responder. Só a própria voz dela continua sendo ignorada.

## v0.0.13 — Ouvir os amigos no Discord

- Nova seção **DISCORD FRIENDS** nas configurações: a Capri escuta o que toca nos seus fones/caixas (a call do Discord) e entende o que seus amigos falam.
- **Auto reply** liga o modo de resposta: ela responde os amigos em voz alta. Em "Answer" você escolhe se ela responde só quando chamam "Capri" ou tudo o que falam.
- **Você vem primeiro**: enquanto você fala, ou ela está cuidando de um pedido seu, a fala dos amigos espera, e a sua voz sempre corta a resposta para eles.
- **Amigos não dão ordens**: a resposta para os amigos não tem ferramentas. Ela só conversa e nunca abre, envia ou muda nada porque um amigo pediu.
- Por voz: "Capri, ouve meus amigos no Discord" / "para de ouvir o Discord", e "liga/desliga o modo de resposta pros amigos".

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
