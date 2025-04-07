using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using ProjektWPF.Data;
using ProjektWPF.Models;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace ProjektWPF
{
    /// <summary>
    /// Logika interakcji dla klasy NotebookView.xaml
    /// </summary>
    public partial class NotebookView : Page
    {
        AppDbContext _context;
        public NotebookView()
        {
            _context = new AppDbContext();
           

            InitializeComponent();
            LoadNotes();
        }

        public void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
        public void LoadNotes()
        {
            NotePanel.Children.Clear(); 
            List<Note> userNotes = _context.Notes.Where(u => u.UserId == Session.User.Id).ToList();

            foreach (var note in userNotes)
            {
                StackPanel noteStack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(10) };

                Rectangle line = new Rectangle
                {
                    Height = 2, 
                    Fill = Brushes.LightBlue, 
                    Width = Double.NaN, 
                    HorizontalAlignment = HorizontalAlignment.Stretch, 
                    Margin = new Thickness(5, 10, 5, 10) 
                };



               
                

           
                TextBlock subjectText = new TextBlock
                {
                    Text = note.Subject,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(10, 0, 0, 0)
                };

                TextBlock contentText = new TextBlock
                {
                    Text = note.Content,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                subjectText.Tag = note.Id;
                subjectText.MouseLeftButtonUp += Note_Clicked;

                noteStack.Children.Add(line);
                noteStack.Children.Add(subjectText);
                noteStack.Children.Add (contentText);
                NotePanel.Children.Add(noteStack);
            }
        }
        private void Note_Clicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock border && border.Tag is int noteId)
            {
                LoadThatNote(noteId);
            }
        }


        public void LoadThatNote(int noteId)
        {
            var note = _context.Notes.FirstOrDefault(n => n.Id == noteId);
            if (note != null)
            {
                TitleBox.Text = note.Subject;
                TitleBox.Tag = noteId;
             
                ContentBox.Document.Blocks.Clear();
                ContentBox.Document.Blocks.Add(new Paragraph(new Run(note.Content)));
            }
            else
            {
                MessageBox.Show("Nie znaleziono notatki.");
            }
        }



        private void SaveNote_Click(object sender, RoutedEventArgs e)
        {
            TextRange textRange = new TextRange(ContentBox.Document.ContentStart, ContentBox.Document.ContentEnd);
            string contentText = textRange.Text.Trim(); 

            if (string.IsNullOrWhiteSpace(TitleBox.Text) || string.IsNullOrWhiteSpace(contentText))
            {
                MessageBox.Show("Tytuł i treść nie mogą być puste!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if(TitleBox.Tag is int noteId)
            {
                var oldNote = _context.Notes.FirstOrDefault(p=>p.Id == noteId);
                oldNote.Subject = TitleBox.Text;
                oldNote.Content = contentText;
                _context.SaveChanges();
                LoadNotes();
                TitleBox.Text = "";
                textRange.Text = "";
                return;
            }
            var newNote = new Note
            {
                Content = contentText,
                Subject = TitleBox.Text,
                UserId = Session.User.Id
            };

            _context.Notes.Add(newNote);
            _context.SaveChanges();

            TitleBox.Text = "";
            textRange.Text = "";
            LoadNotes(); 
        }



        private void LoadNote_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
