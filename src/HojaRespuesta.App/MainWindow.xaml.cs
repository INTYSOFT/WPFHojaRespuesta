using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using HojaRespuesta.App.Models;
using HojaRespuesta.App.Services;
using HojaRespuesta.App.ViewModels;
using HojaRespuesta.Omr.Configuration;
using HojaRespuesta.Omr.Models;
using HojaRespuesta.Omr.Processing;
using Microsoft.Win32;
using OpenCvSharp.WpfExtensions;

namespace HojaRespuesta.App;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly PdfPageLoader _pdfPageLoader = new();
    private readonly OmrEngine _omrEngine = new();
    private readonly OmrTemplateConfig _templateConfig = OmrTemplateConfig.CreateDefault();
    private readonly ObservableCollection<PageSummaryViewModel> _pageSummaries = new();
    private readonly ObservableCollection<AnswerResult> _selectedAnswers = new();
    private readonly List<PageSource> _loadedPages = new();
    private PageSummaryViewModel? _selectedPage;
    private BitmapSource? _selectedPageImage;
    private string _statusMessage = "Listo.";
    private bool _isBusy;
    private string? _currentPdfPath;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public ObservableCollection<PageSummaryViewModel> PageSummaries => _pageSummaries;
    public ObservableCollection<AnswerResult> SelectedAnswers => _selectedAnswers;

    public PageSummaryViewModel? SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (_selectedPage == value)
            {
                return;
            }

            _selectedPage = value;
            OnPropertyChanged(nameof(SelectedPage));
            UpdateSelectedPageDetails();
        }
    }

    public BitmapSource? SelectedPageImage
    {
        get => _selectedPageImage;
        private set
        {
            _selectedPageImage = value;
            OnPropertyChanged(nameof(SelectedPageImage));
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            _statusMessage = value;
            OnPropertyChanged(nameof(StatusMessage));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private async void OnLoadPdfClicked(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "Archivo PDF (*.pdf)|*.pdf",
            Title = "Seleccionar examen"
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadPdfAsync(dialog.FileName);
        }
    }

    private async void OnProcessOmrClicked(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        if (_loadedPages.Count == 0)
        {
            MessageBox.Show("Debes cargar un PDF antes de procesar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await ProcessOmrAsync();
    }

    private async void OnExportCsvClicked(object sender, RoutedEventArgs e)
    {
        if (PageSummaries.Count == 0)
        {
            MessageBox.Show("No hay resultados para exportar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = "ResultadosOMR.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            await ExportCsvAsync(dialog.FileName);
        }
    }

    private async Task LoadPdfAsync(string filePath)
    {
        try
        {
            SetBusy(true);
            StatusMessage = "Cargando PDF...";
            DisposePages();
            var pages = await Task.Run(() => _pdfPageLoader.Load(filePath));
            foreach (var page in pages)
            {
                _loadedPages.Add(page);
            }

            PageSummaries.Clear();
            SelectedAnswers.Clear();
            SelectedPageImage = null;
            SelectedPage = null;
            _currentPdfPath = filePath;
            StatusMessage = $"PDF cargado ({_loadedPages.Count} páginas).";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar el PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Error al cargar.";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task ProcessOmrAsync()
    {
        try
        {
            SetBusy(true);
            StatusMessage = "Procesando OMR...";
            var summaries = await Task.Run(() =>
            {
                var items = new List<PageSummaryViewModel>();
                foreach (var page in _loadedPages)
                {
                    var result = _omrEngine.ProcessPage(page.Image, page.PageNumber, _templateConfig);
                    items.Add(new PageSummaryViewModel(result, page));
                }

                return items;
            });

            PageSummaries.Clear();
            foreach (var summary in summaries)
            {
                PageSummaries.Add(summary);
            }

            StatusMessage = "Procesamiento finalizado.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error durante el procesado: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Error en el procesado.";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task ExportCsvAsync(string destination)
    {
        try
        {
            StatusMessage = "Exportando CSV...";
            var builder = new StringBuilder();
            builder.AppendLine("Archivo,Página,DNI,Pregunta,Respuesta");
            var pdfName = _currentPdfPath is null ? string.Empty : Path.GetFileName(_currentPdfPath);

            await Task.Run(() =>
            {
                foreach (var summary in PageSummaries)
                {
                    foreach (var answer in summary.Result.Answers)
                    {
                        var answerValue = answer.SelectedOption?.ToString() ?? string.Empty;
                        builder.AppendLine($"{pdfName},{summary.PageNumber},{summary.Dni},{answer.QuestionNumber},{answerValue}");
                    }
                }

                File.WriteAllText(destination, builder.ToString(), Encoding.UTF8);
            });

            StatusMessage = "CSV exportado.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo exportar el CSV: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Error al exportar.";
        }
    }

    private void UpdateSelectedPageDetails()
    {
        SelectedAnswers.Clear();
        if (SelectedPage == null)
        {
            SelectedPageImage = null;
            return;
        }

        foreach (var answer in SelectedPage.Result.Answers)
        {
            SelectedAnswers.Add(answer);
        }

        SelectedPageImage = BitmapSourceConverter.ToBitmapSource(SelectedPage.Source.Image);
        SelectedPageImage?.Freeze();
    }

    private void DisposePages()
    {
        foreach (var page in _loadedPages)
        {
            page.Dispose();
        }

        _loadedPages.Clear();
    }

    private void SetBusy(bool value)
    {
        _isBusy = value;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        DisposePages();
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
