using System;
using System.Threading.Tasks;
using System.Windows;
using MySql.Data.MySqlClient;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
//using System.Windows.Controls;
using System.Threading;
using Org.BouncyCastle.Asn1.Crmf;
using System.ComponentModel;
using System.Collections.ObjectModel;



namespace AntivirusApp
{
    class Virus
    {
        private string virusName;
        private string virusHash;

        public void setVirusName(string virusName)
        {
            this.virusName = virusName;
        }
        public void setVirusHash(string virusHash)
        {
            this.virusHash = virusHash;
        }
        public string getVirusName()
        {
            return virusName;
        }
        public string getVirusHash()
        {
            return virusHash;
        }
    }

    class VirusFile : INotifyPropertyChanged
    {
        public string virusName { get; set; }
        public string filepath { get; set; }
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
        
    }

    // 디렉터리 탐색하면서 파일 정보 저장할 클래스
    class Files
    {
        private string fileName;
        private string fileHash;

        public void setFileName(string fileName)
        {
            this.fileName = fileName;
        }
        public void setHash(string fileHash)
        {
            this.fileHash = fileHash;
        }
        public string getFileName()
        {
            return fileName;
        }

        public string getHash()
        {
            return fileHash;
        }
    }

    public partial class ResultWindow : Window
    {
        // 프로젝트 솔루션 폴더에 virus.txt 넣고 돌리기
        private string virusInfoFile = "C:\\Users\\junee\\OneDrive\\바탕 화면\\virus.txt";
        private ArrayList fileList = null; // 파일 정보 담을 리스트
        // 악성코드로 판단된 파일 저장할 리스트
        private ObservableCollection<VirusFile> deleteFileList = null;
        // 바이러스 정보 담는 해쉬 자료구조
        private Dictionary<string, string> virusList = new Dictionary<string, string>();
        private string connectionString = "Server=localhost;Database=project;Uid=root;Pwd=;";
        private MySqlConnection connection;

        private int totalFiles = 0; // 총 파일 수 (임의의 값)
        private int maliciousFiles = 0; // 악성 파일 수 (임의의 값)
        private int currentFile = 0; // 현재까지 검사 완료된 파일 수
        private TimeSpan elapsedTime = TimeSpan.Zero; // 검사 진행 시간
        private DateTime startTime;
        // 진행률 바 함수
        BackgroundWorker _worker = null;

        private static int cnt = 0;

        
        //public object ProgressBarStyle { get; private set; }

        public ResultWindow()
        {
            InitializeComponent();
            virusListView.DataContext = this;
            Start();
        }
        private async void Start()
        {
            _worker = new BackgroundWorker();
            _worker.WorkerReportsProgress = true;
            _worker.DoWork += _worker_DoWork;
            _worker.ProgressChanged += _worker_ProgressChanged;
            _worker.RunWorkerCompleted += _worker_RunWorkerCompleted;

            uiPb_Main.Minimum = 0;
            uiPb_Main.Value = 0;
            uiPb_Main.Maximum = 100;

            connection = new MySqlConnection(connectionString);
            connection.Open();
            string sql = "insert into lasttime values()";
            MySqlCommand command = new MySqlCommand(sql, connection);
            if (command.ExecuteNonQuery() == 1)
            {
                Console.WriteLine($"성공");
            }
            else
            {
                Console.WriteLine($"실패");
            }
            // 검사 시작 시간 저장
            startTime = DateTime.Now;
            UpdateProgress();

            // 바이러스 정보 로딩이랑 파일 스캔 작업 동시에 진행
            Task loadTask = Task.Run(() =>
            {
                loadVirusFile();
            });

            cnt = 0;
            fileList = new ArrayList();
            deleteFileList = new ObservableCollection<VirusFile>();

            await Task.Run(() =>
            {
                SimulateFileScan();
                loadTask.Wait();
                Console.WriteLine($"스캔 끝");
            });
            
            totalFiles = fileList.Count;

            Console.WriteLine($"virus : {virusList.Count}");

            _worker.RunWorkerAsync();
            connection.Close();
        }

        private void loadVirusFile()
        {
            Console.WriteLine($"load virus");
            // 바이러스 정보 읽어온 후 해쉬에 저장
            StreamReader reader = new StreamReader(virusInfoFile);
            string line;
            int i = 0;
            while ((line = reader.ReadLine()) != null)
            {
                string[] parts = line.Split(' ');
                // key - value : hash - 바이러스이름
                virusList[parts[1]] = parts[0];
                i++;
                // 파일 스캔이 먼저 끝날 경우, 시간 UI 업데이트 자연스럽게 되도록
                // 너무 빨리 업데이트하면 자원 낭비, 느리게 업데이트 하면 시간 끊김
                if (i % 100000 == 0)
                {
                    lock (fileList)
                    {
                        UpdateProgress();
                    }
                }
            }
            reader.Close();
        }

        // 파일 검사 시뮬레이션
        private void SimulateFileScan()
        {
            Console.WriteLine($"filescan start");
            Random random = new Random();

            lock (fileList)
            {
                UpdateProgress();
            }

            // 이거 쓰레드로 할지 말지 고민 중
            // 디스크별로 파일 탐색 후 파일 경로명과 해쉬값을 리스트에 저장
            foreach (string dir in MainWindow.filepath)
            {
                searchDir(dir);
            }
        }

        // UI 업데이트
        private void UpdateProgress()
        {
            // UI에 현재까지 검사 완료된 파일 수, 전체 파일 수, 악성 파일 수, 진행률 표시
            elapsedTime = DateTime.Now - startTime;
            txtTotalFiles.Dispatcher.Invoke(() =>
            {
                //Console.WriteLine($"전체 파일 개수 : {totalFiles}");
                txtTotalFiles.Text = $"전체 파일 수: {totalFiles}";
            });
            txtMaliciousFiles.Dispatcher.Invoke(() =>
            {
                txtMaliciousFiles.Text = $"악성 파일 수: {maliciousFiles}";
            });
            txtProgress.Dispatcher.Invoke(() =>
            {
                if (cnt == 0) 
                {
                    txtProgress.Text = $"진행률: 0%";
                }
                else
                {
                    txtProgress.Text = $"진행률: {(int)(currentFile / (double)(totalFiles + 1) * 100)}%";

                
                
                }
            });
            txtElapsedTime.Dispatcher.Invoke(() =>
            {
                if (cnt == 0)
                {
                    txtElapsedTime.Text = $"검사 진행 시간: 00:00";
                }
                else
                {
                    txtElapsedTime.Text = $"검사 진행 시간: " +
                    $"{(int)(elapsedTime.TotalSeconds/60)}:" +
                    $"{(int)(elapsedTime.TotalSeconds%60)}";
                }
            });
            if (cnt == 0)
                cnt++;
        }
        
        private void _worker_DoWork(object sender, DoWorkEventArgs e)
        {
            // 파일의 해쉬값이 바이러스 리스트에 존재하는지 확인 후, 존재하면 출력
            for (int i = 0; i < fileList.Count; i++)
            {
                Files tmp = (Files)fileList[i];
                currentFile++;
                // 진행률 업데이트
                _worker.ReportProgress(currentFile);
                if (virusList.ContainsKey(tmp.getHash()))
                {
                    Dispatcher.Invoke((System.Action)(() =>
                    {
                        deleteFileList.Add(
                            new VirusFile()
                            {
                                virusName = virusList[tmp.getHash()],
                                filepath = tmp.getFileName()
                            }
                            );
                    }));
                    //악성파일리스트에출력
                    Console.WriteLine($"탐지한 악성코드명 : {virusList[tmp.getHash()]} \n" +
                        $"해당 파일 경로 : {tmp.getFileName()}\n\n");
                    maliciousFiles++;
                }
            }
            Console.WriteLine($"End");
            currentFile = totalFiles + 1;
            UpdateProgress();
        }

        private void _worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            UpdateProgress();
            Console.WriteLine($"count : {deleteFileList.Count}");
            Console.WriteLine($"percent : {e.ProgressPercentage}");
            if (deleteFileList.Count > 0)
            {
                virusListView.ItemsSource = deleteFileList;
            }
            uiPb_Main.Value = e.ProgressPercentage;
        }
        
        private void _worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            uiPb_Main.Value = uiPb_Main.Maximum;
        }
        

        private void GoHomeButton_Click(object sender, RoutedEventArgs e)
        {
            // 홈으로 돌아가는 버튼 클릭 시 홈 창 열기
            MainWindow homeWindow = new MainWindow();
            homeWindow.Show();
            this.Close();
        }


        private void Recover_Click(object sender, RoutedEventArgs e)
        {
            //치료하기 버튼 클릭시에..!
            // UI 변경 코드 추가
            caring.Visibility = Visibility.Visible;
            // 테스트 몇 번 해봐야 될 것 같은데 진짜 삭제하면 못하니까 삭제 코드 주석처리함
            // 테스트 코드로 삭제 기능 돌아가는 건 시험해봤음
            /*
            for (int i = 0; i < deleteFileList.Count; i++)
            {
                VirusFile tmp = (VirusFile)deleteFileList[i];
                File.Delete(tmp.filepath);
            }
            */
            caring.Text = "치료 완료";
        }


        
        private void detectedVirusList_Click(object sender, RoutedEventArgs e)
        {
            
            this.Close();
        }
        


        // 내용 읽어서 해쉬값 생성하려는 파일이 다른 프로세스에서 사용 중인지 등을 체크
        private bool CheckFileLocked(string filePath)
        {
            FileStream fs = null;

            try
            {
                fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception)
            {
                // 기존 IOException
                //에러가 발생한 이유는 이미 다른 프로세서에서 점유중이거나.
                //혹은 파일이 존재하지 않기 때문이다. ==> Exception으로 바꿔서 권한 문제까지
                return true;
            }
            finally
            {
                if (fs != null)
                {
                    //만약에 파일이 정상적으로 열렸다면 점유중이 아니다.
                    //다시 파일을 닫아줘야 한다.
                    fs.Close();
                }
            }
            return false;
        }

        private void searchDir(string directory)
        {
            string[] dirs = null, files = null;

            // 여기서부터 쭉 특정 폴더 내의 폴더 리스트, 파일 리스트 각각 추출
            lock (fileList)
            {
                UpdateProgress();
            }
            try
            {
                dirs = Directory.GetDirectories(directory);
            }
            catch (UnauthorizedAccessException e)
            {
                return;
            }
            catch (IOException e)
            {

            }

            try
            {
                files = Directory.GetFiles(directory);
            }
            catch (UnauthorizedAccessException e)
            {
                return;
            }
            catch (IOException e)
            {

            }

            // 위에서 추출한 폴더 리스트를 다시 쓰레드로 병렬 재귀 탐색
            if (dirs != null)
            {
                
                Parallel.ForEach(dirs, dir =>
                {
                    searchDir(dir);
                });
            }

            // 위에서 추출한 파일 리스트를 쓰레드로 병렬 해쉬 추출
            if (files != null)
            {
             // 중첩 Task는 불안정해서 Parallel만 중첩으로 함   
                //await Task.Run(() => {
                    Parallel.ForEach(files, file =>
                    {
                        if (!CheckFileLocked(file))
                        {
                            Files tmpFile = new Files();
                            tmpFile.setFileName(file);
                            tmpFile.setHash(getMD5Hash(file));
                            lock (fileList)
                            {
                                totalFiles++;
                                fileList.Add(tmpFile);
                            }
                        }
                    });

                //});
            }
            lock (fileList)
            {
                UpdateProgress();
            }
        }

        // 파일에서 해쉬 추출
        private string getMD5Hash(string filename)
        {
            FileStream stream = File.OpenRead(filename);
            byte[] data = MD5.Create().ComputeHash(stream);
            stream.Close();
            return BitConverter.ToString(data).Replace("-", "").ToLowerInvariant();
        }
    }
}

