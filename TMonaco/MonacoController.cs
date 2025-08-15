// Decompiled with JetBrains decompiler
// Type: TMonaco.MonacoController
// Assembly: TMonaco, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: D54B0D40-E386-462B-8F7B-8FE4604D1269
// Assembly location: G:\Sgamma_user\SGamma2_2.5.0\Samsun.SGamma\bin\Debug\TMonaco.dll

using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

#nullable enable
namespace TMonaco;

public class MonacoController
{
  private const string EditorContainerSelector = "#root";
  public const string EditorObject = "wpfUiMonacoEditor";
  private readonly Microsoft.Web.WebView2.Wpf.WebView2 _webView;

  public MonacoController(Microsoft.Web.WebView2.Wpf.WebView2 webView) => this._webView = webView;

  public async Task CreateAsync()
  {
    string str = await this._webView.ExecuteScriptAsync("const wpfUiMonacoEditor = monaco.editor.create(document.querySelector('#root'));\r\nwindow.onresize = () => {wpfUiMonacoEditor.layout();}");
  }

  public async Task SetThemeAsync(ApplicationTheme appApplicationTheme)
  {
    string str = await this._webView.ExecuteScriptAsync($"monaco.editor.defineTheme('{"wpf-ui-app-theme"}', {{\r\n    base: '{(appApplicationTheme == ApplicationTheme.Light ? "vs" : "vs-dark")}',\r\n    inherit: true,\r\n    rules: [{{ background: 'FFFFFF00' }}],\r\n    colors: {{'editor.background': '#FFFFFF00','minimap.background': '#FFFFFF00',}}}});\r\nmonaco.editor.setTheme('{"wpf-ui-app-theme"}');");
  }

  public async Task SetLanguageAsync(MonacoLanguage monacoLanguage)
  {
    string str = await this._webView.ExecuteScriptAsync($"monaco.editor.setModelLanguage(wpfUiMonacoEditor.getModel(), \"{(monacoLanguage == MonacoLanguage.ObjectiveC ? "objective-c" : monacoLanguage.ToString().ToLower())}\");");
  }

  public async Task SetContentAsync(string contents)
  {
    SymbolDisplay.FormatLiteral(contents, false);
    contents = JsonSerializer.Serialize<string>(contents);
    string str = await this._webView.ExecuteScriptAsync($"wpfUiMonacoEditor.setValue({contents});");
  }

  public void DispatchScript(string script)
  {
    if (this._webView == null)
      return;
    Application.Current.Dispatcher.InvokeAsync<Task<string>>((Func<Task<string>>) (async () => await this._webView.ExecuteScriptAsync(script)));
  }

  public async Task<string> GetContentAsync()
  {
    return JsonSerializer.Deserialize<string>(await this._webView.ExecuteScriptAsync("wpfUiMonacoEditor.getValue();"));
  }
}
