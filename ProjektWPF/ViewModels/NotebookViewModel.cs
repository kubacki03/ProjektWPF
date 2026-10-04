using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ProjektWPF.Data;
using ProjektWPF.Models;
using ProjektWPF.Services;

namespace ProjektWPF.ViewModels
{
    public class NotebookViewModel : ViewModelBase
    {
        private readonly INavigationService _navigation;
        private readonly IDialogService _dialogs;

        private int? _editedNoteId;
        private string _title = "";
        private string _content = "";

        public ObservableCollection<Note> Notes { get; } = new ObservableCollection<Note>();

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public ICommand SaveNoteCommand { get; }
        public ICommand LoadNoteCommand { get; }
        public ICommand BackCommand { get; }

        public NotebookViewModel(INavigationService navigation, IDialogService dialogs)
        {
            _navigation = navigation;
            _dialogs = dialogs;

            SaveNoteCommand = new AsyncRelayCommand(SaveNoteAsync);
            LoadNoteCommand = new AsyncRelayCommand(LoadNoteAsync);
            BackCommand = new RelayCommand(() => _navigation.GoBack());

            _ = LoadNotesAsync();
        }

        private async Task LoadNotesAsync()
        {
            await using var context = new AppDbContext();
            int userId = Session.User.Id;

            var notes = await context.Notes.Where(n => n.UserId == userId).ToListAsync();

            Notes.Clear();
            foreach (var note in notes)
            {
                Notes.Add(note);
            }
        }

        private async Task LoadNoteAsync(object? parameter)
        {
            if (parameter is not int noteId)
            {
                return;
            }

            await using var context = new AppDbContext();
            var note = await context.Notes.FirstOrDefaultAsync(n => n.Id == noteId);

            if (note == null)
            {
                _dialogs.ShowWarning("Nie znaleziono notatki.");
                return;
            }

            _editedNoteId = note.Id;
            Title = note.Subject;
            Content = note.Content;
        }

        private async Task SaveNoteAsync()
        {
            string content = Content.Trim();

            if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(content))
            {
                _dialogs.ShowWarning("Tytuł i treść nie mogą być puste!");
                return;
            }

            await using (var context = new AppDbContext())
            {
                Note? note = _editedNoteId.HasValue
                    ? await context.Notes.FirstOrDefaultAsync(n => n.Id == _editedNoteId.Value)
                    : null;

                if (note == null)
                {
                    note = new Note { UserId = Session.User.Id };
                    context.Notes.Add(note);
                }

                note.Subject = Title;
                note.Content = content;
                await context.SaveChangesAsync();
            }

            _editedNoteId = null;
            Title = "";
            Content = "";
            await LoadNotesAsync();
        }
    }
}
