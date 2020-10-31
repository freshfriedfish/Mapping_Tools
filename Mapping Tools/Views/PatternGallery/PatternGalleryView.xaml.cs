﻿using Mapping_Tools.Classes;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Classes.Tools.PatternGallery;
using Mapping_Tools.Components.Dialogs.CustomDialog;
using Mapping_Tools.Viewmodels;
using MaterialDesignThemes.Wpf;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Editor_Reader;

namespace Mapping_Tools.Views.PatternGallery {
    /// <summary>
    /// Interactielogica voor PatternGalleryView.xaml
    /// </summary>
    [SmartQuickRunUsage(SmartQuickRunTargets.Always)]
    [HiddenTool]
    public partial class PatternGalleryView : ISavable<PatternGalleryVm>, IQuickRun, IHaveExtraProjectMenuItems {
        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "patterngalleryproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Pattern Gallery Projects");

        public static readonly string ToolName = "Pattern Gallery";
        public static readonly string ToolDescription =
            $@"Save and load patterns from osu! beatmaps.";

        /// <summary>
        /// 
        /// </summary>
        public PatternGalleryView()
        {
            InitializeComponent();
            DataContext = new PatternGalleryVm();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            ProjectManager.LoadProject(this, message: false);
            InitializeOsuPatternFileHandler();
        }

        public PatternGalleryVm ViewModel => (PatternGalleryVm)DataContext;

        public event EventHandler RunFinished;

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var bgw = sender as BackgroundWorker;
            e.Result = ExportPattern((PatternGalleryVm) e.Argument, bgw, e);
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            RunTool(ViewModel.ExportTimeMode == ExportTimeMode.Current
                ? new[] { IOHelper.GetCurrentBeatmapOrCurrentBeatmap() }
                : MainWindow.AppWindow.GetCurrentMaps(), quick: false);
        }

        public void QuickRun()
        {
            RunTool(new[] {IOHelper.GetCurrentBeatmapOrCurrentBeatmap()}, quick: true);
        }

        private void RunTool(string[] paths, bool quick = false)
        {
            if (!CanRun) return;

            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            BackupManager.SaveMapBackup(paths);

            ViewModel.Paths = paths;
            ViewModel.Quick = quick;

            BackgroundWorker.RunWorkerAsync(DataContext);

            CanRun = false;
        }

        private string ExportPattern(PatternGalleryVm args, BackgroundWorker worker, DoWorkEventArgs _) {
            EditorReader reader;
            double exportTime = 0;
            bool usePatternOffset = false;
            switch (args.ExportTimeMode) {
                case ExportTimeMode.Current:
                    reader = EditorReaderStuff.GetFullEditorReader();
                    exportTime = reader.EditorTime();
                    break;
                case ExportTimeMode.Pattern:
                    reader = EditorReaderStuff.GetFullEditorReaderOrNot();
                    usePatternOffset = true;
                    break;
                case ExportTimeMode.Custom:
                    reader = EditorReaderStuff.GetFullEditorReaderOrNot();
                    exportTime = args.CustomExportTime;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ExportTimeMode), "Invalid value encountered");
            }
           
            var editor = EditorReaderStuff.GetNewestVersionOrNot(args.Paths[0], reader);

            var patternCount = args.Patterns.Count(o => o.IsSelected);

            if (patternCount == 0)
                throw new Exception("No pattern has been selected to export.");

            var patternPlacer = args.OsuPatternPlacer;
            foreach (var pattern in args.Patterns.Where(o => o.IsSelected)) {
                var patternBeatmap = pattern.GetPatternBeatmap(args.FileHandler);

                if (usePatternOffset) {
                    patternPlacer.PlaceOsuPattern(patternBeatmap, editor.Beatmap, protectBeatmapPattern:false);
                } else {
                    patternPlacer.PlaceOsuPatternAtTime(patternBeatmap, editor.Beatmap, exportTime, false);
                }

                // Increase pattern use count and time
                pattern.UseCount++;
                pattern.LastUsedTime = DateTime.Now;
            }

            editor.SaveFile();

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(100);

            // Do stuff
            if (args.Quick)
                RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, reader != null));

            return "Successfully exported pattern!";
        }

        private void InitializeOsuPatternFileHandler() {
            // Make sure the file handler always uses the right pattern files folder
            if (ViewModel.FileHandler != null) {
                ViewModel.FileHandler.BasePath = DefaultSaveFolder;
                ViewModel.FileHandler.EnsureCollectionFolderExists();
            }
        }

        public PatternGalleryVm GetSaveData()
        {
            return (PatternGalleryVm) DataContext;
        }

        public void SetSaveData(PatternGalleryVm saveData)
        {
            DataContext = saveData;
            InitializeOsuPatternFileHandler();
        }

        public MenuItem[] GetMenuItems() {
            var menu = new MenuItem {
                Header = "_Rename collection", Icon = new PackIcon { Kind = PackIconKind.Rename },
                ToolTip = "Rename this collection and the collection's directory in the Pattern Files directory."
            };
            menu.Click += DoRenameCollection;

            return new[] { menu };
        }

        private async void DoRenameCollection(object sender, RoutedEventArgs e) {
            try {
                var viewModel = new CollectionRenameVm {
                    NewName = ViewModel.CollectionName, 
                    NewFolderName = ViewModel.FileHandler.CollectionFolderName
                };

                var dialog = new CustomDialog(viewModel, 0);
                var result = await DialogHost.Show(dialog, "RootDialog");

                if (!(bool)result) return;

                ViewModel.CollectionName = viewModel.NewName;
                ViewModel.FileHandler.RenameCollectionFolder(viewModel.NewFolderName);

                await Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue("Successfully renamed this collection!"));
            } catch (ArgumentException) { } catch (Exception ex) {
                ex.Show();
            }
        }
    }
}