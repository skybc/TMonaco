using Microsoft.Web.WebView2.Core;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TMonaco;

/// <summary>
/// TMonacoEditor 是一个基于 WebView2 的 WPF 控件，集成 Monaco 编辑器。
/// 支持内容、语言、主题设置等功能。
/// </summary>
public class TMonacoEditor : Microsoft.Web.WebView2.Wpf.WebView2
{
    /// <summary>
    /// Monaco 编辑器控制器，用于与 Monaco 编辑器进行交互。
    /// </summary>
    private MonacoController? _monacoController;

    /// <summary>
    /// 当 Monaco 编辑器加载状态发生变化时触发的事件。
    /// </summary>
    public event EventHandler MonacoIsLoadedChanged;

    /// <summary>
    /// 获取或设置 Monaco 编辑器是否已加载。
    /// </summary>
    public bool MonacoIsLoaded { get; set; }

    /// <summary>
    /// 构造函数，初始化控件并异步加载 Monaco 编辑器。
    /// </summary>
    public TMonacoEditor() => this.InitializeAsync();

    /// <summary>
    /// 异步初始化 WebView2 和 Monaco 编辑器相关资源。
    /// </summary>
    private async void InitializeAsync()
    {
        TMonacoEditor webView = this;
        // 创建 Monaco 控制器实例
        webView._monacoController = new MonacoController((Microsoft.Web.WebView2.Wpf.WebView2)webView);
        // 初始化 WebView2 环境
        await (webView.EnsureCoreWebView2Async((CoreWebView2Environment)null, (CoreWebView2ControllerOptions)null));
        // 注册导航完成事件
        webView.NavigationCompleted += new EventHandler<CoreWebView2NavigationCompletedEventArgs>(webView.OnWebViewNavigationCompleted);
        // 设置控件属性
        webView.SetCurrentValue(FrameworkElement.UseLayoutRoundingProperty, (object)true);
        webView.SetCurrentValue(Microsoft.Web.WebView2.Wpf.WebView2.DefaultBackgroundColorProperty, (object)Color.Transparent);
        // 设置 Monaco 编辑器页面路径
        webView.SetCurrentValue(Microsoft.Web.WebView2.Wpf.WebView2.SourceProperty, (object)new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets\\Monaco\\index.html")));
    }

    /// <summary>
    /// 异步初始化 Monaco 编辑器，包括主题、语言和内容设置。
    /// </summary>
    private async Task InitializeEditorAsync()
    {
        TMonacoEditor sender = this;
        if (sender._monacoController == null)
            return;
        // 创建 Monaco 编辑器实例
        await sender._monacoController.CreateAsync();
        // 设置编辑器主题为 Light
        await sender._monacoController.SetThemeAsync(ApplicationTheme.Light);
        // 设置编辑器语言为 Csharp
        await sender._monacoController.SetLanguageAsync(MonacoLanguage.Csharp);
        // 设置编辑器内容为空
        await sender._monacoController.SetContentAsync("");
        // 标记编辑器已加载
        sender.MonacoIsLoaded = true;
        // 触发加载状态变化事件
        EventHandler monacoIsLoadedChanged = sender.MonacoIsLoadedChanged;
        if (monacoIsLoadedChanged == null)
            return;
        monacoIsLoadedChanged((object)sender, new EventArgs());
    }

    /// <summary>
    /// WebView2 导航完成事件处理，触发 Monaco 编辑器初始化。
    /// </summary>
    /// <param name="sender">事件源。</param>
    /// <param name="e">导航完成事件参数。</param>
    private void OnWebViewNavigationCompleted(
      object? sender,
      CoreWebView2NavigationCompletedEventArgs e)
    {
        TMonacoEditor.DispatchAsync<Task>(new Func<Task>(this.InitializeEditorAsync));
    }

    /// <summary>
    /// 在主线程异步调度指定的回调方法。
    /// </summary>
    /// <typeparam name="TResult">回调返回值类型。</typeparam>
    /// <param name="callback">要执行的回调方法。</param>
    /// <returns>调度操作对象。</returns>
    private static DispatcherOperation<TResult> DispatchAsync<TResult>(Func<TResult> callback)
    {
        return Application.Current.Dispatcher.InvokeAsync<TResult>(callback);
    }

    /// <summary>
    /// 异步获取 Monaco 编辑器中的内容。
    /// </summary>
    /// <returns>编辑器内容字符串。</returns>
    public async Task<string> GetContentAsync()
    {
        return await this._monacoController?.GetContentAsync() ?? "";
    }

    /// <summary>
    /// 异步设置 Monaco 编辑器的内容。
    /// </summary>
    /// <param name="content">要设置的内容字符串。</param>
    public async Task SetContentAsync(string content)
    {
        await this._monacoController?.SetContentAsync(content);
    }

    /// <summary>
    /// 异步设置 Monaco 编辑器的语言。
    /// </summary>
    /// <param name="language">要设置的语言枚举值。</param>
    public async Task SetLanguageAsync(MonacoLanguage language)
    {
        await this._monacoController?.SetLanguageAsync(language);
    }

    /// <summary>
    /// 异步格式化 Monaco 编辑器中的文档内容。
    /// </summary>
    public async Task Format()
    {
        string str = await this.ExecuteScriptAsync("wpfUiMonacoEditor.getAction('editor.action.formatDocument').run();");
    }

    /// <summary>
    /// 自定义提示，输入为程序集（多个），解析程序集里面全部的类、属性、方法
    /// </summary>
    /// <param name="assemblyNames">要解析的程序集名称列表。</param>
    public async Task SetCustomSuggestAsync(Assembly[] assemblyNames)
    {
        if (this._monacoController == null)
        {
            return;
        }
        StringBuilder script = new StringBuilder(1024 * 5);
        script.AppendLine("monaco.languages.registerCompletionItemProvider('csharp', {");
        script.AppendLine("  provideCompletionItems: (model, position) => {");

        script.AppendLine("    var suggestions = [];");
        // 解析程序集，获取类型信息
        foreach (var assembly in assemblyNames)
        {
            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    // 添加类名
                    script.AppendLine($"    suggestions.push({{");
                    script.AppendLine($"      label: '{type.Name}',");
                    script.AppendLine($"      kind: monaco.languages.CompletionItemKind.Class,");
                    script.AppendLine($"      insertText: '{type.Name}'");
                    script.AppendLine("    });");
                    // 添加方法,方法需要在class后面才可以使用

                    foreach (var method in type.GetMethods())
                    {
                        script.AppendLine($"    suggestions.push({{");
                        script.AppendLine($"      label: '{method.Name}',");
                        //  insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet
                        script.AppendLine($"      insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,");
                        script.AppendLine($"      kind: monaco.languages.CompletionItemKind.Method,");
                        script.AppendLine($"      insertText: '{method.Name}()'");
                        script.AppendLine("    });");
                    }
                    // 添加属性
                    foreach (var property in type.GetProperties())
                    {
                        script.AppendLine($"    suggestions.push({{");
                        script.AppendLine($"      label: '{property.Name}',");
                        script.AppendLine($"      insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,");
                        script.AppendLine($"      kind: monaco.languages.CompletionItemKind.Property,");
                        script.AppendLine($"      insertText: '{property.Name}'");
                        script.AppendLine("    });");
                    }
                }
            }catch(Exception ex)
            {
                // 
            }
        }



        script.AppendLine("    return {");
        script.AppendLine("      suggestions: suggestions");
        script.AppendLine("    };");
        script.AppendLine("  }");
        script.AppendLine("});");

        // 执行脚本，将自定义提示注册到 Monaco 编辑器
        await this.ExecuteScriptAsync(script.ToString());



        // 现在对Monaco进行二次开发，现在有程序集，需要构建程序集的提示词（类名，属性，方法，字段等）
        /*

monaco.languages.registerCompletionItemProvider('sql', {
  provideCompletionItems: (
    model,
    position,
    ) => {
       
      }
      return {
            suggestions: [
                {
                label: 'IWorkflowDesign',
                kind: monaco.languages.CompletionItemKind.Keyword,
                insertText: 'IWorkflowDesign',
                }
            ],
            };
      }
    }
})
 
        */


    }




}
