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

            // ListView에서 디렉터리 더블클릭(또는 Enter) 처리
            lvwLeftDir.ItemActivate += lvwLeftDir_ItemActivate;
            lvwRightDir.ItemActivate += lvwRightDir_ItemActivate;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        // 현재 선택된 최상위(루트) 폴더(탐색의 기준)
        private string baseLeftRoot = string.Empty;
        private string baseRightRoot = string.Empty;

        private void btnLeftDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "폴더를 선택하세요.";
                if (!string.IsNullOrWhiteSpace(txtLeftDir.Text) && Directory.Exists(txtLeftDir.Text))
                {
                    dlg.SelectedPath = txtLeftDir.Text;
                }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtLeftDir.Text = dlg.SelectedPath;

                    // 왼쪽에서 선택 시
                    baseLeftRoot = dlg.SelectedPath;
                    PopulateListView(lvwLeftDir, dlg.SelectedPath);

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
                if (!string.IsNullOrWhiteSpace(txtRightDir.Text) && Directory.Exists(txtRightDir.Text))
                {
                    dlg.SelectedPath = txtRightDir.Text;
                }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtRightDir.Text = dlg.SelectedPath;

                    // 오른쪽에서 선택 시
                    baseRightRoot = dlg.SelectedPath;
                    PopulateListView(lvwRightDir, dlg.SelectedPath);

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
                // 기존 otherFolder 계산 대신
                string otherFolder = string.Empty;
                if (lv == lvwLeftDir)
                {
                    // left view일 때, otherFolder는 오른쪽 루트 + left의 현재 폴더에 대한 상대경로
                    if (!string.IsNullOrWhiteSpace(baseLeftRoot) && !string.IsNullOrWhiteSpace(baseRightRoot) && IsSubPath(folderPath, baseLeftRoot))
                    {
                        var rel = Path.GetRelativePath(baseLeftRoot, folderPath);
                        otherFolder = Path.Combine(baseRightRoot, rel);
                    }
                    else
                    {
                        otherFolder = txtRightDir.Text;
                    }
                }
                else
                {
                    // right view일 때
                    if (!string.IsNullOrWhiteSpace(baseRightRoot) && !string.IsNullOrWhiteSpace(baseLeftRoot) && IsSubPath(folderPath, baseRightRoot))
                    {
                        var rel = Path.GetRelativePath(baseRightRoot, folderPath);
                        otherFolder = Path.Combine(baseLeftRoot, rel);
                    }
                    else
                    {
                        otherFolder = txtLeftDir.Text;
                    }
                }
                bool otherFolderExists = !string.IsNullOrWhiteSpace(otherFolder) && Directory.Exists(otherFolder);

                // 디렉터리 항목: 하위 전체를 하나의 항목처럼 취급 -> 마지막 수정일은 하위 중 최신 시간으로 계산
                var dirs = Directory.EnumerateDirectories(folderPath)
                    .Select(p => new DirectoryInfo(p))
                    .OrderBy(d => d.Name);
                foreach (var d in dirs)
                {
                    var item = new ListViewItem(d.Name);
                    item.SubItems.Add("<DIR>");
                    var aggregatedTime = GetDirectoryLastWriteTimeSafe(d.FullName);
                    item.SubItems.Add(aggregatedTime.ToString("g"));

                    if (otherFolderExists)
                    {
                        string otherDirPath = Path.Combine(otherFolder, d.Name);
                        if (Directory.Exists(otherDirPath))
                        {
                            var otherTime = GetDirectoryLastWriteTimeSafe(otherDirPath);
                            if (aggregatedTime == otherTime)
                                item.ForeColor = Color.Black; // 동일
                            else
                                item.ForeColor = aggregatedTime > otherTime ? Color.Red : Color.Gray; // New / Old
                        }
                        else
                        {
                            item.ForeColor = Color.Purple; // 단독
                        }
                    }
                    else
                    {
                        item.ForeColor = Color.Purple;
                    }

                    lv.Items.Add(item);
                }

                // 파일 항목
                var files = Directory.EnumerateFiles(folderPath)
                    .Select(p => new FileInfo(p))
                    .OrderBy(f => f.Name);
                foreach (var f in files)
                {
                    var item = new ListViewItem(f.Name);
                    item.SubItems.Add(f.Length.ToString("N0") + " 바이트");
                    item.SubItems.Add(f.LastWriteTime.ToString("g"));

                    if (otherFolderExists)
                    {
                        string otherFilePath = Path.Combine(otherFolder, f.Name);
                        if (File.Exists(otherFilePath))
                        {
                            var rf = new FileInfo(otherFilePath);
                            if (f.LastWriteTime == rf.LastWriteTime)
                                item.ForeColor = Color.Black;
                            else
                                item.ForeColor = f.LastWriteTime > rf.LastWriteTime ? Color.Red : Color.Gray;
                        }
                        else
                        {
                            item.ForeColor = Color.Purple;
                        }
                    }
                    else
                    {
                        item.ForeColor = Color.Purple;
                    }

                    lv.Items.Add(item);
                }

                for (int i = 0; i < lv.Columns.Count; i++)
                {
                    lv.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.ColumnContent);
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

        // 선택된 항목(파일/폴더)을 상대편으로 복사. 완료 시 요약 메시지 한 번만 표시.
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
                MessageBox.Show(this, "복사할 항목을 좌측 목록에서 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int copied = 0, skipped = 0, failed = 0;
            foreach (var item in selected)
            {
                var name = item.Text;
                if (item.SubItems.Count > 1 && item.SubItems[1].Text == "<DIR>")
                {
                    // 디렉터리 전체 복사
                    var srcDir = Path.Combine(txtLeftDir.Text, name);
                    var destDir = Path.Combine(txtRightDir.Text, name);
                    CopyDirectoryRecursiveWithConfirmation(srcDir, destDir, ref copied, ref skipped, ref failed);
                }
                else
                {
                    var srcPath = Path.Combine(txtLeftDir.Text, name);
                    var destPath = Path.Combine(txtRightDir.Text, name);
                    if (CopyFileWithConfirmation(srcPath, destPath))
                        copied++;
                    else
                        skipped++;
                }
            }

            PopulateListView(lvwLeftDir, txtLeftDir.Text);
            PopulateListView(lvwRightDir, txtRightDir.Text);

            
        }

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
                MessageBox.Show(this, "복사할 항목을 우측 목록에서 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int copied = 0, skipped = 0, failed = 0;
            foreach (var item in selected)
            {
                var name = item.Text;
                if (item.SubItems.Count > 1 && item.SubItems[1].Text == "<DIR>")
                {
                    var srcDir = Path.Combine(txtRightDir.Text, name);
                    var destDir = Path.Combine(txtLeftDir.Text, name);
                    CopyDirectoryRecursiveWithConfirmation(srcDir, destDir, ref copied, ref skipped, ref failed);
                }
                else
                {
                    var srcPath = Path.Combine(txtRightDir.Text, name);
                    var destPath = Path.Combine(txtLeftDir.Text, name);
                    if (CopyFileWithConfirmation(srcPath, destPath))
                        copied++;
                    else
                        skipped++;
                }
            }

            PopulateListView(lvwLeftDir, txtLeftDir.Text);
            PopulateListView(lvwRightDir, txtRightDir.Text);

            
        }

        // 파일 복사 전 확인(존재 시 수정일 비교)
        private bool CopyFileWithConfirmation(string srcPath, string destPath)
        {
            try
            {
                var src = new FileInfo(srcPath);
                if (!src.Exists)
                {
                    // 파일 없음 -> 실패로 처리 (디렉터리 복사 루틴에서 적절히 카운트)
                    return false;
                }

                if (File.Exists(destPath))
                {
                    var dest = new FileInfo(destPath);

                    if (src.LastWriteTime > dest.LastWriteTime)
                    {
                        var msg = $"대상 파일보다 소스 파일이 최신입니다.\n\n파일: {Path.GetFileName(destPath)}\n소스 수정일: {src.LastWriteTime}\n대상 수정일: {dest.LastWriteTime}\n\n덮어쓰시겠습니까?";
                        var dr = MessageBox.Show(this, msg, "덮어쓰기 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (dr != DialogResult.Yes)
                            return false;
                    }
                    else
                    {
                        var msg = $"대상 파일이 더 최신이거나 동일합니다.\n\n파일: {Path.GetFileName(destPath)}\n소스 수정일: {src.LastWriteTime}\n대상 수정일: {dest.LastWriteTime}\n\n강제로 덮어쓰시겠습니까?";
                        var dr = MessageBox.Show(this, msg, "덮어쓰기 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (dr != DialogResult.Yes)
                            return false;
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? string.Empty);
                File.Copy(src.FullName, destPath, true);
                File.SetLastWriteTime(destPath, src.LastWriteTime);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "복사 중 오류가 발생했습니다: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // 디렉터리 전체를 재귀적으로 복사. 파일 단위로 CopyFileWithConfirmation을 호출해 덮어쓰기 확인을 유지.
        private void CopyDirectoryRecursiveWithConfirmation(string srcDir, string destDir, ref int copied, ref int skipped, ref int failed)
        {
            try
            {
                if (!Directory.Exists(srcDir))
                {
                    failed++;
                    return;
                }

                // 만들기
                Directory.CreateDirectory(destDir);

                // 파일 복사
                foreach (var file in Directory.EnumerateFiles(srcDir))
                {
                    var destPath = Path.Combine(destDir, Path.GetFileName(file));
                    var ok = CopyFileWithConfirmation(file, destPath);
                    if (ok) copied++;
                    else skipped++;
                }

                // 하위 디렉터리 재귀
                foreach (var dir in Directory.EnumerateDirectories(srcDir))
                {
                    var name = Path.GetFileName(dir);
                    if (name == null) continue;
                    var nextDest = Path.Combine(destDir, name);
                    CopyDirectoryRecursiveWithConfirmation(dir, nextDest, ref copied, ref skipped, ref failed);
                }
            }
            catch
            {
                failed++;
            }
        }

        // 디렉터리의 하위 파일/폴더를 재귀로 탐색하여 가장 최신의 수정시간을 반환 (접근 예외 안전 처리)
        private DateTime GetDirectoryLastWriteTimeSafe(string dir)
        {
            try
            {
                DateTime max = Directory.GetLastWriteTime(dir);

                foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var t = File.GetLastWriteTime(file);
                        if (t > max) max = t;
                    }
                    catch { /* 접근 오류 시 무시 */ }
                }

                return max;
            }
            catch
            {
                return Directory.GetLastWriteTime(dir);
            }
        }

        // 디렉터리 더블클릭 시 해당 디렉터리로 이동
        private void lvwLeftDir_ItemActivate(object sender, EventArgs e)
        {
            var item = lvwLeftDir.SelectedItems.Cast<ListViewItem>().FirstOrDefault();
            if (item == null || item.SubItems.Count == 0 || item.SubItems[1].Text != "<DIR>")
                return; // 디렉터리가 아닐 경우 무시

            // 하위 디렉터리로 경로 변경
            string selectedDir = Path.Combine(baseLeftRoot, item.Text);
            if (Directory.Exists(selectedDir))
            {
                txtLeftDir.Text = selectedDir;
                PopulateListView(lvwLeftDir, selectedDir);
            }
        }

        private void lvwRightDir_ItemActivate(object sender, EventArgs e)
        {
            var item = lvwRightDir.SelectedItems.Cast<ListViewItem>().FirstOrDefault();
            if (item == null || item.SubItems.Count == 0 || item.SubItems[1].Text != "<DIR>")
                return; // 디렉터리가 아닐 경우 무시

            // 하위 디렉터리로 경로 변경
            string selectedDir = Path.Combine(baseRightRoot, item.Text);
            if (Directory.Exists(selectedDir))
            {
                txtRightDir.Text = selectedDir;
                PopulateListView(lvwRightDir, selectedDir);
            }
        }

        // 지정한 경로가 기준 경로의 하위(또는 동일)인지 검사
        private bool IsSubPath(string path, string basePath)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(basePath))
                return false;

            try
            {
                var fullPath = Path.GetFullPath(path)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                var fullBase = Path.GetFullPath(basePath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;

                // Windows에서는 대소문자 구분 없음. 필요 시 비교 방법 변경.
                return fullPath.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}