using Microsoft.AspNetCore.Components;
using SRMApp.Localization;

namespace SRMApp.Components;

public abstract class LocalizedComponentBase : ComponentBase, IDisposable
{
    [Inject]
    protected LanguageService LanguageService { get; set; } = default!;

    protected override void OnInitialized()
    {
        LanguageService.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public virtual void Dispose()
    {
        LanguageService.LanguageChanged -= OnLanguageChanged;
    }
}
