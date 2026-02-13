using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Printing;
using Windows.Graphics.Printing;
using System;
using System.Threading.Tasks;

namespace OpticaPro.Services
{
    public class PrintService
    {
        private readonly nint _hWnd;
        private PrintManager _printManager;
        private PrintDocument _printDocument;
        private IPrintDocumentSource _printDocumentSource;
        private UIElement _elementToPrint;
        private bool _isRegistered = false;

        public PrintService(nint hWnd)
        {
            _hWnd = hWnd;
        }

        public void Register()
        {
            if (_isRegistered) return;

            _printManager = PrintManagerInterop.GetForWindow(_hWnd);
            _printManager.PrintTaskRequested += PrintTaskRequested;

            _printDocument = new PrintDocument();
            _printDocumentSource = _printDocument.DocumentSource;
            _printDocument.Paginate += Paginate;
            _printDocument.GetPreviewPage += GetPreviewPage;
            _printDocument.AddPages += AddPages;

            _isRegistered = true;
        }

        public void Unregister()
        {
            if (!_isRegistered) return;

            if (_printManager != null)
            {
                _printManager.PrintTaskRequested -= PrintTaskRequested;
                _printManager = null;
            }

            // Liberar documento para no dejar basura en memoria
            if (_printDocument != null)
            {
                try { _printDocumentSource = null; } catch { }
                _printDocument = null;
            }

            _isRegistered = false;
        }

        public async Task PrintAsync(UIElement element)
        {
            // Aseguramos que esté registrado
            if (!_isRegistered) Register();

            _elementToPrint = element;

            // Lanzar diálogo de impresión
            try
            {
                await PrintManagerInterop.ShowPrintUIForWindowAsync(_hWnd);
            }
            catch
            {
                // Ignorar errores si el diálogo ya estaba abriéndose
            }
        }

        private void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
        {
            var printTask = args.Request.CreatePrintTask("OpticaPro - Historial Clínico", sourceRequested =>
            {
                sourceRequested.SetSource(_printDocumentSource);
            });
        }

        private void Paginate(object sender, PaginateEventArgs e)
        {
            _printDocument.SetPreviewPageCount(1, PreviewPageCountType.Final);
        }

        private void GetPreviewPage(object sender, GetPreviewPageEventArgs e)
        {
            // AQUÍ OCURRÍA EL ERROR ANTES: Si _elementToPrint no tiene "padre" visual, fallaba.
            // Ahora que lo agregamos a 'PrintingHost', funcionará perfecto.
            if (_elementToPrint != null)
            {
                _printDocument.SetPreviewPage(e.PageNumber, _elementToPrint);
            }
        }

        private void AddPages(object sender, AddPagesEventArgs e)
        {
            if (_elementToPrint != null)
            {
                _printDocument.AddPage(_elementToPrint);
            }
            _printDocument.AddPagesComplete();
        }
    }
}