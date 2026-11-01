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
