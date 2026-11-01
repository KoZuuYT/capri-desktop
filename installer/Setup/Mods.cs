#if !UNINSTALLER
namespace CapriSetup;

// what this Capri has that the original 0.7.7 doesn't, for the "O QUE MUDOU" page; keep it in step with CHANGELOG.md
internal sealed class Mod
{
	internal const int New = 0;

	internal const int Better = 1;

	internal const int Fixed = 2;

	internal static readonly string[] Tags = { "NOVO", "MELHOR", "CORRIGIDO" };

	internal readonly int Kind;

	internal readonly string Name;

	internal readonly string Text;

	private Mod(int kind, string name, string text)
	{
		Kind = kind;
		Name = name;
		Text = text;
	}

	internal static readonly Mod[] All =
	{
		new Mod(New, "Medidor de carinho", "Ela vai gostando mais de você com o tempo: 5 níveis, de Desconfiada a Inseparável. O jeito dela falar muda com o nível, e o medidor aparece no menu do botão direito."),
		new Mod(New, "Modo travessura", "De vez em quando ela apronta: tela bugada, cursor roubado, aviso falso de hacker, enxame de olhos (com olhos vermelhos), janela puxada ou sacudida, tela virada e pichação. Tudo inofensivo, e dá para escolher qual travessura disparar."),
		new Mod(New, "Ouvir os amigos no Discord", "Ela entende o que seus amigos falam na call e pode responder em voz alta. Amigos não dão ordens: com eles ela só conversa."),
		new Mod(New, "Transmissão de voz", "A voz dela pode entrar no seu microfone (cabo de áudio virtual) para Discord, OBS e gravações, junto com a sua."),
		new Mod(New, "Atalhos por voz", "\"Deita\", \"fica quieta\", \"se esconde\", \"aparece\", \"sobe no meu mouse\" e \"desce do mouse\", cada um com raios, som e uma fala dela."),
		new Mod(New, "Lembretes, Pomodoro e hora", "\"Capri, me lembra de beber água em 30 minutos.\" Focos de 25/5 ou 50/10 minutos, e ela fala a hora de hora em hora."),
		new Mod(New, "Alertas do PC", "Avisa quando a CPU passa de 90% por muito tempo e quando a bateria cai para 20% fora da tomada."),
		new Mod(New, "Olhos pela tela", "Olhos no estilo dela surgem pela tela, seguem o cursor e piscam. Frequência, tamanho e quantidade ajustáveis no Mainframe."),
		new Mod(New, "Carinho com o mouse", "Esfregue o cursor nela: corações de pixel, anéis neon, um acorde digital e às vezes um ronronar \"~prrrr.exe~\"."),
		new Mod(New, "Física ao arrastar", "Ela balança como um pêndulo quando você segura e pode ser arremessada, girando e quicando nas bordas da tela."),
		new Mod(New, "Raios no estilo do anime", "Raios vermelhos em zigue-zague com glitch, faíscas e sons novos ao teleportar, pular, ser arremessada e cair."),
		new Mod(New, "Perfis e estatísticas", "Perfis Calm, Normal e Chaos, botões para testar raio, carinho e hora, e contadores de carinhos, ronronadas, arremessos e dias juntos."),
		new Mod(New, "Saudação em português", "Ao ligar ela diz \"Bom dia, choom.\" (e as outras saudações) na voz que você escolheu, em vez da gravação em inglês."),
		new Mod(New, "Changelog dentro dela", "Uma janela neon com todas as mudanças do mod, aberta pelo Mainframe."),
		new Mod(New, "Dar arquivos pra ela comer", "Arraste um arquivo até ela: o ícone voa pra boca dela, vira farelo de pixel e ela comenta o sabor. Nada é apagado."),
		new Mod(New, "Minijogo Pega o olho", "30 segundos clicando nos olhos dela pela tela: verde vale 1, vermelho vale 3, combo e recorde."),
		new Mod(New, "Esconde-esconde", "Ela se esconde atrás de uma borda da tela e espia de vez em quando. Ache e clique nela."),
		new Mod(New, "Humor do dia e sono à noite", "Todo dia ela acorda Sonolenta, Elétrica, Carente ou Rabugenta; depois da meia-noite ela dorme se você largar o PC."),
		new Mod(New, "Conquistas", "18 conquistas com troféus de pixel, cartão dourado ao desbloquear e janela com o progresso de cada uma."),
		new Mod(New, "Avisos neon e boot do Mainframe", "Lembretes, Pomodoro, CPU, bateria e conquistas no mesmo cartão neon; o Mainframe liga como um terminal antigo."),
		new Mod(Better, "Stats com nível e XP", "Janela de stats nova com nível, XP, títulos, números animados que atualizam na hora, conquistas juntas e comentários dela sobre seus números."),
		new Mod(Better, "Comer arquivos mais fácil", "Uma rede invisível em volta dela pega arquivos soltos perto, com animação de sucção e sons que mudam conforme o arquivo se aproxima."),
		new Mod(Better, "Sons no tempo certo", "Os efeitos sonoros se misturam ao vivo, então no minijogo e no enxame de olhos eles tocam junto com o que aparece na tela."),
		new Mod(Better, "Entrada e saída em pixels", "Ela aparece e some se desfazendo em blocos de pixel com scanlines."),
		new Mod(Better, "Balão de fala estilo terminal", "Barra de título capri@k.mod com status ao vivo e glitches enquanto ela fala."),
		new Mod(New, "Instalador K.mod", "Este instalador, com opções detalhadas, e uma versão para Linux (Fedora) que roda a Capri pelo Wine."),
		new Mod(Better, "Ouvido mais esperto", "O som da call passa por um filtro de verdade; ela reconhece \"Cap\", \"Capre\", \"Cabre\" e junta o nome com a pergunta que vem logo depois."),
		new Mod(Better, "Mainframe redimensionável", "Puxe qualquer borda ou canto, de 75% a 200%, com uma prévia neon animada do tamanho final."),
		new Mod(Better, "Carinho mais preciso", "Só conta um vai-e-vem de verdade, então ele não dispara com qualquer tremidinha do mouse."),
		new Mod(Better, "Botão TYPE/TALK ao lado dela", "O botão fica ao lado da Capri (ou do outro lado, perto da borda) e não mais em cima dela."),
		new Mod(Better, "Português mais natural", "Tradução ajustada, com gírias como \"corp\" no lugar de \"corpo\"."),
		new Mod(Better, "Menu com a cara do mod", "O menu do botão direito mostra \"CAPRI / K.mod\", o medidor de carinho e as travessuras."),
		new Mod(Fixed, "Atualizações oficiais bloqueadas", "O app oficial não baixa mais uma versão por cima do mod (dá para desligar no Mainframe)."),
		new Mod(Fixed, "Sem travadas no Windows", "O mouse do Windows não engasga mais quando a Capri está ocupada."),
		new Mod(Fixed, "Nome escrito certo", "Quando o reconhecimento de voz escreve \"capre\", \"cápria\" ou \"kapri\", o texto vira \"Capri\".")
	};
}
#endif
