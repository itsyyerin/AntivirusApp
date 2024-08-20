// MainWindow.xaml.cs

using System;
using System.Windows;
using System.Collections.Generic;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using MySql.Data.MySqlClient;

namespace AntivirusApp
{
    public partial class MainWindow : Window
    {
        // 탐색할 폴더 경로 저장
        public static List<string> filepath = new List<string>();
        private string connectionString = "Server=localhost;Database=project;Uid=root;Pwd=;";
        private MySqlConnection connection;

        public MainWindow()
        {
            InitializeComponent();
            connection = new MySqlConnection(connectionString);
            connection.Open();
            // 마지막 검사 시간 하나만 불러옴
            string sql = "select time from lasttime order by time desc limit 1";
            MySqlCommand command = new MySqlCommand(sql, connection);
            MySqlDataReader table = command.ExecuteReader();
            while (table.Read())
            {
                lastScanTime.Text = "마지막 검사 시간 : " + table["time"].ToString();
            }
            table.Close();
        }

        // 전체 검사 버튼 클릭 시
        private void FullScanButton_Click(object sender, RoutedEventArgs e)
        {
            // pc에 마운트 된 모든 디스크 정보 가져오기
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drives)
            {
                filepath.Add(drive.Name);
            }
            OpenResultWindow();
        }

        // 부분 검사 버튼 클릭 시
        private void PartialScanButton_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            // 폴더 탐색기 첫 화면을 C드라이브로
            dialog.InitialDirectory = "C:\\";
            // 폴더 or 파일 선택에서 폴더 선택으로
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                filepath.Add(dialog.FileName);
                OpenResultWindow();
            }
        }

        // 예약 검사 버튼 클릭 시
        private void ScheduledScanButton_Click(object sender, RoutedEventArgs e)
        {
            OpenResultWindow();
        }

        // 결과 창 열기
        private void OpenResultWindow()
        {
            ResultWindow resultWindow = new ResultWindow();
            resultWindow.Show();
            this.Close();
        }
    }
}

