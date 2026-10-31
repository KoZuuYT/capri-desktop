using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;

namespace CapriDesktop;

internal sealed class WindowsDictation : Dictation
{
	private readonly object gate = new object();

	private SpeechRecognizer recognizer;

	private bool closed;

	private bool running;

	private bool held;

	private bool busy;

	private bool opened;

	private DateTime began;

	private int quick;

	internal static string Probe()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected Obj, but got Unknown
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected Obj, but got Unknown
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			SpeechRecognizer val = new SpeechRecognizer();
			try
			{
				val.Constraints.Add((ISpeechRecognitionConstraint)new SpeechRecognitionTopicConstraint((SpeechRecognitionScenario)1, "dictation"));
				SpeechRecognitionCompilationResult result = val.CompileConstraintsAsync().AsTask<SpeechRecognitionCompilationResult>().Result;
				return ((int)result.Status == 0) ? null : string.Concat("Windows speech recognition couldn't load (", result.Status, ").");
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception error)
		{
			return Dictation.Explain(error);
		}
	}

	internal override void Start(Action<string> started)
	{
		ThreadPool.QueueUserWorkItem((object param0) =>
		{
			string obj = null;
			try
			{
				Open();
			}
			catch (Exception error)
			{
				obj = Dictation.Explain(error);
				Close();
			}
			started(obj);
		});
	}

	internal static string Tag(string language, IList<string> supported, string region)
	{
		if (language == null || supported == null)
		{
			return null;
		}
		string text = Brain.LanguageCode(language);
		string text2;
		if (language == "Traditional Chinese")
		{
			text2 = "TW|HK|MO|Hant";
		}
		else
		{
			text2 = ((language == "Simplified Chinese") ? "CN|SG|Hans" : null);
		}
		string text3 = null;
		foreach (string item in supported)
		{
			if (string.IsNullOrEmpty(item))
			{
				continue;
			}
			string[] array = item.Split('-');
			string text4 = array[0].ToLowerInvariant();
			if ((!(text4 != text) || (text == "no" && (text4 == "nb" || text4 == "nn"))) && (text2 == null || Regex.IsMatch(item, "-(" + text2 + ")(-|$)", RegexOptions.IgnoreCase)))
			{
				if (!string.IsNullOrEmpty(region) && array.Length > 1 && string.Equals(array[array.Length - 1], region, StringComparison.OrdinalIgnoreCase))
				{
					return item;
				}
				if (text3 == null)
				{
					text3 = item;
				}
			}
		}
		return text3;
	}

	private static Language Tongue(string language)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		if (language == null)
		{
			return null;
		}
		try
		{
			List<string> list = new List<string>();
			foreach (Language supportedTopicLanguage in SpeechRecognizer.SupportedTopicLanguages)
			{
				list.Add(supportedTopicLanguage.LanguageTag);
			}
			string text = Tag(language, list, RegionInfo.CurrentRegion.TwoLetterISORegionName);
			return (text == null) ? ((Language)null) : new Language(text);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private void Open()
	{
		// ILSpy could not decompile this member: Error decompiling @0600089A CapriDesktop.WindowsDictation.Open: Value cannot be null. (Parameter 'source')
		// Please report the exception below,
		// along with the assembly it came from, at https://github.com/icsharpcode/ILSpy/issues/new
		// Error decompiling @0600089A CapriDesktop.WindowsDictation.Open
		// in assembly "C:/Users/Usuário/AppData/Local/Capri/backup-before-shortcut-cleanup/Capri.exe.orig"
		//  ---> System.ArgumentNullException: Value cannot be null. (Parameter 'source')
		//    at System.Linq.ThrowHelper.ThrowArgumentNullException(ExceptionArgument) + 0x31
		//    at System.Linq.Enumerable.Take[TSource](IEnumerable`1, Int32) + 0x48
		//    at ICSharpCode.Decompiler.CSharp.ArgumentList.GetArgumentResolveResultsDirect(Int32) + 0x24
		//    at ICSharpCode.Decompiler.CSharp.Disambiguator.Probe() + 0xc8
		//    at ICSharpCode.Decompiler.CSharp.Disambiguator.Run() + 0x10
		//    at ICSharpCode.Decompiler.CSharp.Disambiguator.ForMethodReference(ExpressionBuilder, IMethod, IType, TranslatedExpression, Boolean, ExpectedTargetDetails, Boolean, Boolean) + 0x103
		//    at ICSharpCode.Decompiler.CSharp.CallBuilder.DisambiguateDelegateReference(IMethod, IMethod, ExpectedTargetDetails, ILInstruction) + 0x25f
		//    at ICSharpCode.Decompiler.CSharp.CallBuilder.BuildDelegateReference(IMethod, IMethod, ExpectedTargetDetails, ILInstruction) + 0x53
		//    at ICSharpCode.Decompiler.CSharp.ExpressionBuilder.VisitLdFtn(LdFtn, TranslationContext) + 0x98
		//    at ICSharpCode.Decompiler.CSharp.ExpressionBuilder.Translate(ILInstruction, IType) + 0xa7
		//    at ICSharpCode.Decompiler.CSharp.CallBuilder.BuildArgumentList(ExpectedTargetDetails, ResolveResult, IMethod, Int32, IReadOnlyList`1, IReadOnlyList`1, Boolean) + 0x38f
		//    at ICSharpCode.Decompiler.CSharp.CallBuilder.HandleDelegateConstruction(CallInstruction) + 0x139
		//    at ICSharpCode.Decompiler.CSharp.CallBuilder.Build(CallInstruction, IType) + 0x9f
		//    at ICSharpCode.Decompiler.CSharp.ExpressionBuilder.VisitNewObj(NewObj, TranslationContext) + 0x150
		//    at ICSharpCode.Decompiler.CSharp.ExpressionBuilder.Translate(ILInstruction, IType) + 0xa7
		//    at ICSharpCode.Decompiler.CSharp.CallBuilder.BuildArgumentList(ExpectedTargetDetails, ResolveResult, IMethod, Int32, IReadOnlyList`1, IReadOnlyList`1, Boolean) + 0x38f
		//    at ICSharpCode.Decompiler.CSharp.CallBuilder.Build(OpCode, IMethod, IReadOnlyList`1, IReadOnlyList`1, IType) + 0x4e7
		//    at ICSharpCode.Decompiler.CSharp.CallBuilder.Build(CallInstruction, IType) + 0x12f
		//    at ICSharpCode.Decompiler.CSharp.ExpressionBuilder.VisitCall(Call, TranslationContext) + 0x57
		//    at ICSharpCode.Decompiler.CSharp.StatementBuilder.Default(ILInstruction) + 0xa7
		//    at ICSharpCode.Decompiler.CSharp.StatementBuilder.ConvertBlockContainer(BlockStatement, BlockContainer, IEnumerable`1, Boolean) + 0x26b
		//    at ICSharpCode.Decompiler.CSharp.StatementBuilder.ConvertBlockContainer(BlockContainer, Boolean) + 0x47
		//    at ICSharpCode.Decompiler.CSharp.StatementBuilder.VisitBlockContainer(BlockContainer) + 0x94
		//    at ICSharpCode.Decompiler.CSharp.StatementBuilder.Convert(ILInstruction) + 0x5d
		//    at ICSharpCode.Decompiler.CSharp.StatementBuilder.ConvertAsBlock(ILInstruction) + 0x11
		//    at ICSharpCode.Decompiler.CSharp.CSharpDecompiler.DecompileBody(IMethod, EntityDeclaration, DecompileRun, ITypeResolveContext, ExtensionInfo) + 0x426
		// -- continuing with outer exception (ICSharpCode.Decompiler.DecompilerException) --
		// 
	}

	internal override void Hold(bool hold)
	{
		lock (gate)
		{
			held = hold;
			if (busy || closed || !opened || running == !held)
			{
				return;
			}
			busy = true;
		}
		ThreadPool.QueueUserWorkItem((object param0) =>
		{
			Settle();
		});
	}

	private void Settle()
	{
		while (true)
		{
			SpeechRecognizer val;
			bool flag;
			lock (gate)
			{
				if (closed || running == !held)
				{
					busy = false;
					break;
				}
				val = recognizer;
				flag = (running = !running);
				if (flag)
				{
					began = DateTime.UtcNow;
				}
			}
			try
			{
				if (flag)
				{
					val.ContinuousRecognitionSession.StartAsync().AsTask().Wait();
				}
				else
				{
					val.ContinuousRecognitionSession.StopAsync().AsTask().Wait();
				}
			}
			catch (Exception error)
			{
				lock (gate)
				{
					busy = false;
				}
				Fail(Dictation.Explain(error));
				break;
			}
		}
	}

	private void Result(SpeechContinuousRecognitionSession session, SpeechContinuousRecognitionResultGeneratedEventArgs args)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		SpeechRecognitionResult result = args.Result;
		if ((int)result.Status != 0 || (int)result.Confidence == 3)
		{
			return;
		}
		string text = (result.Text ?? "").Trim();
		lock (gate)
		{
			if (closed || held || text.Length == 0)
			{
				return;
			}
		}
		Heard?.Invoke(text);
	}

	private void Ended(SpeechContinuousRecognitionSession session, SpeechContinuousRecognitionCompletedEventArgs args)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I4
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Invalid comparison between Unknown and I4
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Invalid comparison between Unknown and I4
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		SpeechRecognitionResultStatus status = args.Status;
		bool flag = (int)status == 10 || (int)status == 1 || (int)status == 2 || (int)status == 3;
		lock (gate)
		{
			if (closed || !running)
			{
				return;
			}
			running = false;
			quick = ((DateTime.UtcNow - began < TimeSpan.FromSeconds(5.0)) ? (quick + 1) : 0);
			if (!flag && quick < 3)
			{
				if (busy)
				{
					return;
				}
				busy = true;
				bool pause = quick > 0;
				ThreadPool.QueueUserWorkItem((object param0) =>
				{
					if (pause)
					{
						Thread.Sleep(700);
					}
					Settle();
				});
				return;
			}
		}
		WindowsDictation windowsDictation = this;
		string problem;
		if ((int)status == 9)
		{
			problem = "Windows speech recognition lost its internet connection.";
		}
		else
		{
			problem = (((int)status == 10) ? "Windows speech recognition couldn't use the mic." : string.Concat("Windows speech recognition stopped (", status, ")."));
		}
		windowsDictation.Fail(problem);
	}

	private void Fail(string problem)
	{
		if (Close())
		{
			Lost?.Invoke(problem);
		}
	}

	private bool Close()
	{
		SpeechRecognizer current;
		lock (gate)
		{
			if (closed)
			{
				return false;
			}
			closed = true;
			current = recognizer;
			recognizer = null;
		}
		if (current == null)
		{
			return true;
		}
		ThreadPool.QueueUserWorkItem((object param0) =>
		{
			try
			{
				current.ContinuousRecognitionSession.CancelAsync().AsTask().Wait(3000);
			}
			catch (Exception)
			{
			}
			try
			{
				current.Dispose();
			}
			catch (Exception)
			{
			}
		});
		return true;
	}

	public override void Dispose()
	{
		Close();
	}
}
