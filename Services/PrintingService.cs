using Microsoft.JSInterop;

namespace NewAttendanceProject.Services
{
    public interface IPrintingService
    {
        Task PrintAsync(string elementId);
    }
    
    public class PrintingService : IPrintingService
    {
        private readonly IJSRuntime _jsRuntime;

        public PrintingService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task PrintAsync(string elementId)
        {
            await _jsRuntime.InvokeVoidAsync("printElement", elementId);
        }
    }
} 