using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FileCompare
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // 디자이너에서 클릭 이벤트가 연결되어 있지 않으면 여기서 연결
            btnCopyFromLeft.Click += btnCopyFromLeft_Click;
            btnCopyFromRight.Click += btnCopyFromRight_Click;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnLeftDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "폴더를 선택하세요.";
                // 현재 텍스트박스에 있는 경로를 초기 선택 폴더로 설정
                if (!string.IsNullOrWhiteSpace(txtLeftDir.Text) && Directory.Exists(txtLeftDir.Text))
                {
                    dlg.SelectedPath = txtLeftDir.Text;
                }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtLeftDir.Text = dlg.SelectedPath;
                    PopulateListView(lvwLeftDir, dlg.SelectedPath);

                    // 반대편도 갱신: 오른쪽 경로가 유효하면 오른쪽 ListView도 갱신
                    if (!string.IsNullOrWhiteSpace(txtRightDir.Text) && Directory.Exists(txtRightDir.Text))
                    {
                        PopulateListView(lvwRightDir, txtRightDir.Text);
                    }
                }
            }
        }
        private void btnRightDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "폴더를 선택하세요.";
                // 현재 텍스트박스에 있는 경로를 초기 선택 폴더로 설정
                if (!string.IsNullOrWhiteSpace(txtRightDir.Text) && Directory.Exists(txtRightDir.Text))
                {
                    dlg.SelectedPath = txtRightDir.Text;
                }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtRightDir.Text = dlg.SelectedPath;
                    // 수정: 오른쪽 버튼은 오른쪽 ListView를 채우도록 수정
                    PopulateListView(lvwRightDir, dlg.SelectedPath);

                    // 반대편도 갱신: 왼쪽 경로가 유효하면 왼쪽 ListView도 갱신
                    if (!string.IsNullOrWhiteSpace(txtLeftDir.Text) && Directory.Exists(txtLeftDir.Text))
                    {
                        PopulateListView(lvwLeftDir, txtLeftDir.Text);
                    }
                }
            }
        }
        private void PopulateListView(ListView lv, string folderPath)
        {
            lv.BeginUpdate();
            lv.Items.Clear();
            try
            {
                // 비교 대상(반대편) 폴더 경로 결정
                string otherFolder = lv == lvwLeftDir ? txtRightDir.Text : txtLeftDir.Text;
                bool otherFolderExists = !string.IsNullOrWhiteSpace(otherFolder) && Directory.Exists(otherFolder);

                // 폴더(디렉터리) 먼저 추가
                var dirs = Directory.EnumerateDirectories(folderPath)
                    .Select(p => new DirectoryInfo(p))
                    .OrderBy(d => d.Name);
                foreach (var d in dirs)
                {
                    var item = new ListViewItem(d.Name);
                    item.SubItems.Add("<DIR>");
                    item.SubItems.Add(d.LastWriteTime.ToString("g"));

                    // 1단계: 이름 비교 -> 같은 이름이 반대편에 있는지 확인
                    if (otherFolderExists)
                    {
                        string otherDirPath = Path.Combine(otherFolder, d.Name);
                        if (Directory.Exists(otherDirPath))
                        {
                            var rd = new DirectoryInfo(otherDirPath);
                            // 2단계: 날짜 비교, 3단계: 상태 결정
                            if (d.LastWriteTime == rd.LastWriteTime)
                            {
                                // 동일
                                item.ForeColor = Color.Black;
                            }
                            else
                            {
                                // 다른 파일: 현재측이 최신이면 New(빨강), 오래된 쪽이면 Old(회색)
                                item.ForeColor = d.LastWriteTime > rd.LastWriteTime ? Color.Red : Color.Gray;
                            }
                        }
                        else
                        {
                            // 단독 디렉터리
                            item.ForeColor = Color.Purple;
                        }
                    }
                    else
                    {
                        // 반대편 폴더가 없으면 단독으로 간주
                        item.ForeColor = Color.Purple;
                    }

                    lv.Items.Add(item);
                }

                // 파일 추가
                var files = Directory.EnumerateFiles(folderPath)
                    .Select(p => new FileInfo(p))
                    .OrderBy(f => f.Name);
                foreach (var f in files)
                {
                    var item = new ListViewItem(f.Name);
                    item.SubItems.Add(f.Length.ToString("N0") + " 바이트");
                    item.SubItems.Add(f.LastWriteTime.ToString("g"));

                    // 1단계: 이름 비교 -> 같은 이름이 반대편에 있는지 확인
                    if (otherFolderExists)
                    {
                        string otherFilePath = Path.Combine(otherFolder, f.Name);
                        if (File.Exists(otherFilePath))
                        {
                            var rf = new FileInfo(otherFilePath);
                            // 2단계: 날짜 비교, 3단계: 상태 결정
                            if (f.LastWriteTime == rf.LastWriteTime)
                            {
                                // 동일
                                item.ForeColor = Color.Black;
                            }
                            else
                            {
                                // 다른 파일: 현재측이 최신이면 New(빨강), 오래된 쪽이면 Old(회색)
                                item.ForeColor = f.LastWriteTime > rf.LastWriteTime ? Color.Red : Color.Gray;
                            }
                        }
                        else
                        {
                            // 단독 파일
                            item.ForeColor = Color.Purple;
                        }
                    }
                    else
                    {
                        // 반대편 폴더가 없으면 단독으로 간주
                        item.ForeColor = Color.Purple;
                    }

                    lv.Items.Add(item);
                }

                // 컬럼 너비 자동 조정 (컨텐츠 기준)
                for (int i = 0; i < lv.Columns.Count; i++)
                {
                    lv.AutoResizeColumn(i,
                    ColumnHeaderAutoResizeStyle.ColumnContent);
                }
            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show(this, "폴더를 찾을 수 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show(this, "입출력 오류: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                lv.EndUpdate();
            }
        }

        // 왼쪽 선택 항목을 오른쪽으로 복사
        private void btnCopyFromLeft_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLeftDir.Text) || !Directory.Exists(txtLeftDir.Text))
            {
                MessageBox.Show(this, "좌측 폴더가 유효하지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtRightDir.Text) || !Directory.Exists(txtRightDir.Text))
            {
                MessageBox.Show(this, "우측 폴더가 유효하지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var selected = lvwLeftDir.SelectedItems.Cast<ListViewItem>().ToList();
            if (!selected.Any())
            {
                MessageBox.Show(this, "복사할 파일을 좌측 목록에서 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (var item in selected)
            {
                // 디렉터리는 건너뜀
                if (item.SubItems.Count > 1 && item.SubItems[1].Text == "<DIR>")
                    continue;

                var name = item.Text;
                var srcPath = Path.Combine(txtLeftDir.Text, name);
                var destPath = Path.Combine(txtRightDir.Text, name);

                CopyFileWithConfirmation(srcPath, destPath);
            }

            // 완료 후 양쪽 목록 갱신
            PopulateListView(lvwLeftDir, txtLeftDir.Text);
            PopulateListView(lvwRightDir, txtRightDir.Text);
        }

        // 오른쪽 선택 항목을 왼쪽으로 복사
        private void btnCopyFromRight_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRightDir.Text) || !Directory.Exists(txtRightDir.Text))
            {
                MessageBox.Show(this, "우측 폴더가 유효하지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtLeftDir.Text) || !Directory.Exists(txtLeftDir.Text))
            {
                MessageBox.Show(this, "좌측 폴더가 유효하지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var selected = lvwRightDir.SelectedItems.Cast<ListViewItem>().ToList();
            if (!selected.Any())
            {
                MessageBox.Show(this, "복사할 파일을 우측 목록에서 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (var item in selected)
            {
                if (item.SubItems.Count > 1 && item.SubItems[1].Text == "<DIR>")
                    continue;

                var name = item.Text;
                var srcPath = Path.Combine(txtRightDir.Text, name);
                var destPath = Path.Combine(txtLeftDir.Text, name);

                CopyFileWithConfirmation(srcPath, destPath);
            }

            // 완료 후 양쪽 목록 갱신
            PopulateListView(lvwLeftDir, txtLeftDir.Text);
            PopulateListView(lvwRightDir, txtRightDir.Text);
        }

        // 대상이 존재하면 수정일 비교 후 덮어쓰기 여부를 사용자에게 묻고 복사 진행
        private bool CopyFileWithConfirmation(string srcPath, string destPath)
        {
            try
            {
                var src = new FileInfo(srcPath);
                if (!src.Exists)
                {
                    MessageBox.Show(this, $"소스 파일을 찾을 수 없습니다: {srcPath}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                if (File.Exists(destPath))
                {
                    var dest = new FileInfo(destPath);

                    // src가 dest보다 최신이면 덮어쓸 때 확인
                    if (src.LastWriteTime > dest.LastWriteTime)
                    {
                        var msg = $"대상 파일보다 소스 파일이 최신입니다.\n\n파일: {Path.GetFileName(destPath)}\n소스 수정일: {src.LastWriteTime}\n대상 수정일: {dest.LastWriteTime}\n\n덮어쓰시겠습니까?";
                        var dr = MessageBox.Show(this, msg, "덮어쓰기 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (dr != DialogResult.Yes)
                            return false;
                    }
                    else
                    {
                        // 대상이 더 최신이거나 동일한 경우에도 확인
                        var msg = $"대상 파일이 더 최신이거나 동일합니다.\n\n파일: {Path.GetFileName(destPath)}\n소스 수정일: {src.LastWriteTime}\n대상 수정일: {dest.LastWriteTime}\n\n강제로 덮어쓰시겠습니까?";
                        var dr = MessageBox.Show(this, msg, "덮어쓰기 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (dr != DialogResult.Yes)
                            return false;
                    }
                }

                // 복사 실행 (덮어쓰기 허용)
                File.Copy(src.FullName, destPath, true);
                // 원본의 수정시간을 유지
                File.SetLastWriteTime(destPath, src.LastWriteTime);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "복사 중 오류가 발생했습니다: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}