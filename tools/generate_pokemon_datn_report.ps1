param(
    [string]$OutputPath = "Bao_cao_DATN_Game_Pokemon.docx"
)

$ErrorActionPreference = "Stop"

function Add-Paragraph {
    param(
        [Parameter(Mandatory=$true)][string]$Text,
        [int]$After = 6,
        [bool]$Bold = $false,
        [bool]$Italic = $false
    )
    $script:Selection.Font.Bold = [int]$Bold
    $script:Selection.Font.Italic = [int]$Italic
    $script:Selection.Font.Name = "Times New Roman"
    $script:Selection.Font.Size = 13
    $script:Selection.ParagraphFormat.Alignment = 3
    $script:Selection.ParagraphFormat.SpaceAfter = $After
    $script:Selection.TypeText($Text)
    $script:Selection.TypeParagraph()
    $script:Selection.Font.Bold = 0
    $script:Selection.Font.Italic = 0
}

function Add-Heading {
    param(
        [Parameter(Mandatory=$true)][string]$Text,
        [int]$Level = 1
    )
    $script:Selection.Font.Name = "Times New Roman"
    $script:Selection.Font.Bold = 1
    $script:Selection.Font.Italic = 0
    if ($Level -eq 1) {
        $script:Selection.Font.Size = 16
        $script:Selection.ParagraphFormat.Alignment = 1
    } elseif ($Level -eq 2) {
        $script:Selection.Font.Size = 14
        $script:Selection.ParagraphFormat.Alignment = 0
    } else {
        $script:Selection.Font.Size = 13
        $script:Selection.ParagraphFormat.Alignment = 0
    }
    $script:Selection.ParagraphFormat.SpaceBefore = 12
    $script:Selection.ParagraphFormat.SpaceAfter = 6
    try {
        $script:Selection.Style = "Heading $Level"
    } catch {}
    $script:Selection.TypeText($Text)
    $script:Selection.TypeParagraph()
    try {
        $script:Selection.Style = "Normal"
    } catch {}
    $script:Selection.Font.Bold = 0
}

function Add-PageBreak {
    $script:Selection.InsertBreak(7)
}

function Add-Table {
    param(
        [Parameter(Mandatory=$true)][string[]]$Headers,
        [Parameter(Mandatory=$true)][object[]]$Rows,
        [string]$Caption = ""
    )
    $rowCount = [Math]::Max(1, $Rows.Count) + 1
    $colCount = $Headers.Count
    $range = $script:Selection.Range
    $table = $script:Document.Tables.Add($range, $rowCount, $colCount)
    $table.Borders.Enable = 1
    $table.Range.Font.Name = "Times New Roman"
    $table.Range.Font.Size = 12
    for ($c = 1; $c -le $colCount; $c++) {
        $table.Cell(1, $c).Range.Text = $Headers[$c - 1]
        $table.Cell(1, $c).Range.Bold = 1
    }
    for ($r = 0; $r -lt $Rows.Count; $r++) {
        $values = @($Rows[$r])
        for ($c = 1; $c -le $colCount; $c++) {
            $value = if ($c -le $values.Count) { [string]$values[$c - 1] } else { "" }
            $table.Cell($r + 2, $c).Range.Text = $value
        }
    }
    $script:Selection.SetRange($table.Range.End, $table.Range.End)
    $script:Selection.TypeParagraph()
    if (-not [string]::IsNullOrWhiteSpace($Caption)) {
        Add-Paragraph -Text $Caption -After 8 -Italic $true
    }
}

function Add-Placeholder {
    param([string]$Text)
    Add-Paragraph -Text ("[CẦN BỔ SUNG] " + $Text) -Italic $true
}

$root = (Resolve-Path ".").Path
$fullOutputPath = Join-Path $root $OutputPath

$csFiles = @(Get-ChildItem -Path "Assets/script" -Recurse -Filter *.cs -File)
$codeLines = 0
foreach ($f in $csFiles) {
    $codeLines += (Get-Content -Path $f.FullName | Measure-Object -Line).Lines
}
$sceneNames = @(Get-ChildItem -Path "Assets/Scenes" -Filter *.unity -File | Select-Object -ExpandProperty BaseName)
$pokemonAssets = @(Get-ChildItem -Path "Assets/Game/Resources/PokemonData" -Filter *.asset -File -ErrorAction SilentlyContinue).Count
$moveAssets = @(Get-ChildItem -Path "Assets/Game/Resources/MoveData" -Filter *.asset -File -ErrorAction SilentlyContinue).Count
$itemAssets = @(Get-ChildItem -Path "Assets/Game/Resources/Item" -Filter *.asset -File -ErrorAction SilentlyContinue).Count
$unityVersion = (Get-Content -Path "ProjectSettings/ProjectVersion.txt" | Select-String "m_EditorVersion:" | ForEach-Object { $_.Line.Replace("m_EditorVersion:", "").Trim() })

$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
$script:Document = $word.Documents.Add()
$script:Selection = $word.Selection

$Document.PageSetup.TopMargin = $word.CentimetersToPoints(2.5)
$Document.PageSetup.BottomMargin = $word.CentimetersToPoints(2.5)
$Document.PageSetup.LeftMargin = $word.CentimetersToPoints(3.0)
$Document.PageSetup.RightMargin = $word.CentimetersToPoints(2.0)

$Selection.Font.Name = "Times New Roman"
$Selection.Font.Size = 13
$Selection.ParagraphFormat.LineSpacingRule = 5
$Selection.ParagraphFormat.LineSpacing = 18

$Selection.ParagraphFormat.Alignment = 1
$Selection.Font.Bold = 1
$Selection.Font.Size = 16
$Selection.TypeText("ĐẠI HỌC BÁCH KHOA HÀ NỘI")
$Selection.TypeParagraph()
$Selection.TypeText("TRƯỜNG CÔNG NGHỆ THÔNG TIN VÀ TRUYỀN THÔNG")
$Selection.TypeParagraph()
$Selection.TypeParagraph()
$Selection.Font.Size = 18
$Selection.TypeText("ĐỒ ÁN TỐT NGHIỆP")
$Selection.TypeParagraph()
$Selection.TypeParagraph()
$Selection.Font.Size = 16
$Selection.TypeText("XÂY DỰNG GAME NHẬP VAI 2D LẤY CẢM HỨNG TỪ POKEMON TRÊN UNITY")
$Selection.TypeParagraph()
$Selection.TypeParagraph()
$Selection.Font.Size = 13
$Selection.Font.Bold = 0
$Selection.TypeText("Sinh viên thực hiện: [Họ và tên sinh viên]")
$Selection.TypeParagraph()
$Selection.TypeText("Mã số sinh viên: [MSSV]")
$Selection.TypeParagraph()
$Selection.TypeText("Email: [Email sinh viên]")
$Selection.TypeParagraph()
$Selection.TypeText("Chương trình đào tạo: [Tên chương trình/ngành]")
$Selection.TypeParagraph()
$Selection.TypeText("Giảng viên hướng dẫn: [Học hàm, học vị, họ tên GVHD]")
$Selection.TypeParagraph()
$Selection.TypeText("Khoa/Viện/Trường: [Thông tin đơn vị đào tạo]")
$Selection.TypeParagraph()
$Selection.TypeParagraph()
$Selection.TypeText("HÀ NỘI, 06/2026")
Add-PageBreak

Add-Heading "LỜI CẢM ƠN" 1
Add-Paragraph "Em xin gửi lời cảm ơn tới giảng viên hướng dẫn, các thầy cô trong trường, gia đình và bạn bè đã hỗ trợ em trong quá trình thực hiện đồ án tốt nghiệp. Trong quá trình xây dựng sản phẩm, em có cơ hội hệ thống lại kiến thức về lập trình hướng đối tượng, thiết kế phần mềm, xử lý giao diện, quản lý dữ liệu và kiểm thử trong môi trường Unity. Đồ án cũng giúp em rèn luyện cách phân tích yêu cầu, chia nhỏ bài toán, sửa lỗi theo luồng thực tế và hoàn thiện sản phẩm qua nhiều lần thử nghiệm. Do giới hạn về thời gian và kinh nghiệm, báo cáo và sản phẩm khó tránh khỏi thiếu sót. Em mong nhận được góp ý của thầy cô để tiếp tục hoàn thiện hệ thống trong các phiên bản tiếp theo."
Add-Placeholder "Thay đoạn này bằng lời cảm ơn cá nhân hơn nếu cần, giữ độ dài khoảng 100-150 từ theo mẫu."
Add-PageBreak

Add-Heading "TÓM TẮT NỘI DUNG ĐỒ ÁN" 1
Add-Paragraph "Đồ án tập trung xây dựng một trò chơi nhập vai 2D lấy cảm hứng từ dòng game Pokemon, trong đó người chơi có thể di chuyển trên bản đồ, tương tác với nhân vật không điều khiển, nhận nhiệm vụ, gặp Pokemon hoang dã, tham gia chiến đấu theo lượt, thu phục Pokemon, quản lý đội hình, sử dụng vật phẩm, lưu tiến trình và tiếp tục câu chuyện chính. Bài toán đặt ra không chỉ là tạo một màn chơi đơn lẻ, mà là thiết kế một hệ thống gameplay có nhiều module liên kết với nhau: bản đồ, nhân vật, hội thoại, nhiệm vụ, cờ truyện, trận đấu, dữ liệu Pokemon, túi đồ, Pokedex, kho lưu trữ và hệ thống save/load. Để giải quyết bài toán này, đồ án lựa chọn Unity làm nền tảng phát triển, C# làm ngôn ngữ lập trình và ScriptableObject làm cơ chế tổ chức dữ liệu tĩnh cho Pokemon, chiêu thức, vật phẩm và nhiệm vụ. Phần trạng thái thay đổi trong quá trình chơi được quản lý bằng các lớp runtime và được tuần tự hóa thành dữ liệu lưu. Sản phẩm đã xây dựng được vòng lặp chơi hoàn chỉnh từ khám phá, tương tác, chiến đấu, nhận thưởng đến lưu lại tiến trình. Đóng góp chính của đồ án nằm ở việc thiết kế kiến trúc gameplay theo hướng module, tách dữ liệu khỏi logic xử lý, đồng thời xây dựng được hệ thống nhiệm vụ cốt truyện có thể mở rộng bằng các bước và hành động cấu hình trong Inspector."
Add-Paragraph "Từ khóa: Unity, C#, game 2D, Pokemon-like RPG, ScriptableObject, turn-based battle, quest system, save/load."
Add-PageBreak

Add-Heading "ABSTRACT" 1
Add-Paragraph "This graduation project presents the design and implementation of a 2D role-playing game inspired by Pokemon. The player can explore multiple maps, interact with non-player characters, receive quests, encounter wild creatures, participate in turn-based battles, capture Pokemon, manage a party, use items, record progress in the Pokedex, and save or load the game state. The main challenge is not limited to implementing an isolated gameplay screen, but lies in connecting several gameplay subsystems into a coherent loop. These subsystems include overworld exploration, interaction, dialog, story progression, battle flow, Pokemon data, inventory, storage, Pokedex, and persistence. The project uses Unity as the game engine, C# as the programming language, and ScriptableObject assets to organize static data such as Pokemon definitions, moves, items, and quests. Runtime data is represented by dedicated model classes and serialized into save data when needed. The resulting system provides a playable gameplay loop and a foundation for further content expansion. The key contribution of this project is a modular gameplay architecture that separates game data from execution logic while supporting configurable story sequences, event-driven quest progress, and persistent game state management."
Add-PageBreak

Add-Heading "MỤC LỤC" 1
$tocRange = $Selection.Range
$Document.TablesOfContents.Add($tocRange, $true, 1, 3) | Out-Null
$Selection.EndKey(6) | Out-Null
Add-PageBreak

Add-Heading "DANH MỤC HÌNH VẼ" 1
Add-Paragraph "Hình 2.1. Biểu đồ use case tổng quát của hệ thống game."
Add-Paragraph "Hình 4.1. Kiến trúc tổng quan của hệ thống gameplay."
Add-Paragraph "Hình 4.2. Luồng xử lý khi người chơi gặp Pokemon hoang dã."
Add-Paragraph "Hình 4.3. Luồng xử lý nhiệm vụ cốt truyện."
Add-Placeholder "Chèn ảnh chụp màn hình từ Unity và biểu đồ UML thật sau khi hoàn thiện báo cáo."

Add-Heading "DANH MỤC BẢNG BIỂU" 1
Add-Paragraph "Bảng 2.1. Danh sách nhóm chức năng chính."
Add-Paragraph "Bảng 3.1. Công nghệ và công cụ sử dụng."
Add-Paragraph "Bảng 4.1. Thống kê thành phần mã nguồn và dữ liệu."
Add-Paragraph "Bảng 4.2. Một số ca kiểm thử chức năng chính."

Add-Heading "DANH MỤC THUẬT NGỮ VÀ TỪ VIẾT TẮT" 1
Add-Table @("Thuật ngữ", "Ý nghĩa") @(
    @("RPG", "Role-playing game, thể loại trò chơi nhập vai trong đó người chơi điều khiển nhân vật tham gia vào thế giới và tiến trình câu chuyện."),
    @("NPC", "Non-player character, nhân vật không do người chơi điều khiển."),
    @("UI", "User Interface, giao diện người dùng."),
    @("HUD", "Head-up Display, phần giao diện hiển thị trạng thái trực tiếp khi chơi."),
    @("SO", "ScriptableObject, cơ chế lưu dữ liệu asset trong Unity."),
    @("Prefab", "Mẫu đối tượng có thể tái sử dụng trong Unity."),
    @("Scene", "Không gian/màn chơi trong Unity."),
    @("Quest", "Nhiệm vụ mà người chơi có thể nhận, thực hiện và hoàn thành."),
    @("Story Flag", "Cờ trạng thái dùng để ghi nhận các mốc cốt truyện đã xảy ra.")
) "Bảng 0.1. Danh mục thuật ngữ sử dụng trong báo cáo."
Add-PageBreak

Add-Heading "CHƯƠNG 1. GIỚI THIỆU ĐỀ TÀI" 1
Add-Paragraph "Chương này trình bày bối cảnh hình thành đề tài, mục tiêu phát triển, phạm vi thực hiện và định hướng giải pháp tổng quát. Nội dung chương tập trung làm rõ lý do lựa chọn bài toán xây dựng game nhập vai 2D, những yêu cầu chính mà sản phẩm cần đáp ứng và cách tổ chức báo cáo."

Add-Heading "1.1 Đặt vấn đề" 2
Add-Paragraph "Game nhập vai 2D là một dạng sản phẩm phần mềm có mức độ tổng hợp cao. Một trò chơi thuộc nhóm này không chỉ cần phần hiển thị đồ họa và điều khiển nhân vật, mà còn cần hệ thống luật chơi, quản lý trạng thái, tương tác bản đồ, dữ liệu nhân vật, âm thanh, giao diện, lưu tiến trình và nhiều cơ chế phản hồi theo hành động của người chơi. Vì vậy, việc xây dựng một game 2D hoàn chỉnh là bài toán phù hợp để vận dụng kiến thức lập trình hướng đối tượng, thiết kế phần mềm, tổ chức dữ liệu và kiểm thử chức năng."
Add-Paragraph "Dòng game Pokemon là ví dụ tiêu biểu cho mô hình game nhập vai kết hợp thu thập sinh vật và chiến đấu theo lượt. Vòng lặp chơi cơ bản của dòng game này tương đối rõ ràng: người chơi khám phá bản đồ, gặp Pokemon, chiến đấu, thu phục, huấn luyện đội hình, hoàn thành nhiệm vụ và mở khóa khu vực tiếp theo. Tuy nhiên, khi chuyển vòng lặp này thành một hệ thống phần mềm, độ phức tạp tăng lên đáng kể vì mỗi hành động nhỏ của người chơi đều có thể làm thay đổi nhiều phần của game. Chẳng hạn, một trận đấu kết thúc có thể làm thay đổi HP, EXP, level, move, trạng thái nhiệm vụ, cờ truyện, tiền thưởng, badge, Pokedex và dữ liệu save."
Add-Paragraph "Trong bối cảnh học tập, nhiều demo game 2D thường chỉ dừng ở mức điều khiển nhân vật hoặc một trận đánh đơn giản. Các demo này có giá trị minh họa kỹ thuật nhưng chưa thể hiện đầy đủ cách các hệ thống gameplay phối hợp với nhau. Đề tài này hướng tới việc xây dựng một sản phẩm có cấu trúc hệ thống rõ ràng hơn, trong đó gameplay không phải là một chuỗi script rời rạc mà là tập hợp các module có trách nhiệm riêng và giao tiếp qua các API nội bộ."
Add-Paragraph "Một vấn đề quan trọng khác là khả năng mở rộng nội dung. Với game nhập vai, số lượng Pokemon, chiêu thức, vật phẩm, nhiệm vụ và sự kiện cốt truyện thường lớn hơn nhiều so với số lượng cơ chế xử lý. Nếu mọi dữ liệu đều được viết cứng trong code, việc bổ sung nội dung sẽ khó kiểm soát và dễ phát sinh lỗi. Do đó, đồ án cần một cách tổ chức dữ liệu để người phát triển có thể thêm Pokemon, move, item hoặc quest thông qua asset cấu hình mà không phải sửa nhiều logic lõi."

Add-Heading "1.2 Mục tiêu và phạm vi đề tài" 2
Add-Paragraph "Mục tiêu tổng quát của đề tài là xây dựng một game nhập vai 2D trên Unity có vòng lặp chơi tương đối hoàn chỉnh theo hướng Pokemon-like RPG. Sản phẩm cần cho phép người chơi di chuyển trên nhiều bản đồ, tương tác với môi trường, nói chuyện với NPC, nhận nhiệm vụ, gặp Pokemon hoang dã, tham gia chiến đấu theo lượt, sử dụng vật phẩm, bắt Pokemon, quản lý đội hình, lưu tiến trình và tiếp tục câu chuyện chính."
Add-Paragraph "Về phạm vi chức năng, đồ án tập trung vào các cơ chế gameplay cốt lõi thay vì phát triển toàn bộ nội dung như một game thương mại. Các chức năng được ưu tiên gồm hệ thống điều khiển nhân vật trên bản đồ 2D, hệ thống trigger và tương tác, hệ thống trận đấu theo lượt, dữ liệu Pokemon và chiêu thức, hệ thống vật phẩm, hệ thống nhiệm vụ, hệ thống cờ truyện, Pokedex, kho Pokemon, save/load và một số scene đại diện cho tiến trình khám phá."
Add-Paragraph "Về phạm vi kỹ thuật, đề tài sử dụng Unity phiên bản $unityVersion và ngôn ngữ C#. Dữ liệu tĩnh như Pokemon, move, item và quest được lưu dưới dạng ScriptableObject trong thư mục Resources. Trạng thái runtime như party, storage, vị trí người chơi, tiến độ quest, cờ truyện và các đối tượng đã được thu thập sẽ được đưa vào cấu trúc save data để ghi lại."
Add-Paragraph "Đề tài chưa đặt mục tiêu hoàn thiện toàn bộ cân bằng gameplay, thiết kế đồ họa nguyên bản cho mọi nhân vật, hoặc triển khai chế độ nhiều người chơi. Những phần như tối ưu animation, hiệu ứng chiến đấu nâng cao, AI đối thủ phức tạp, localization đầy đủ và hệ thống build phát hành đa nền tảng được xem là hướng mở rộng sau đồ án."

Add-Heading "1.3 Định hướng giải pháp" 2
Add-Paragraph "Định hướng giải pháp của đồ án là xây dựng hệ thống theo kiến trúc module, trong đó mỗi nhóm chức năng có lớp điều phối và lớp dữ liệu riêng. GameController đóng vai trò điều phối trạng thái tổng thể giữa overworld, battle, menu, dialog và cutscene. BattleSystem phụ trách logic chiến đấu. QuestManager phụ trách trạng thái nhiệm vụ. MainStoryDirector và MainStorySequence phụ trách tiến trình cốt truyện. SaveLoadSystem phụ trách lưu và khôi phục dữ liệu."
Add-Paragraph "Đối với dữ liệu nội dung, đồ án lựa chọn ScriptableObject để mô tả PokemonBase, MoveBase, ItemBase và Quest. Cách tiếp cận này phù hợp với Unity vì dữ liệu có thể được chỉnh sửa trực tiếp trong Inspector, được tái sử dụng qua nhiều scene và ít phụ thuộc vào code runtime. Logic xử lý chỉ tham chiếu tới dữ liệu này thông qua các thuộc tính công khai, nhờ đó giảm việc viết cứng dữ liệu trong mã nguồn."
Add-Paragraph "Đối với tiến trình cốt truyện, đồ án sử dụng cơ chế story flag và main story sequence. Mỗi step trong chuỗi cốt truyện có scene, trigger, điều kiện flag và danh sách action. Nhờ đó, việc tạo thêm một đoạn cốt truyện mới có thể thực hiện bằng cách cấu hình step và action, thay vì viết riêng một director cho từng sự kiện."

Add-Heading "1.4 Bố cục đồ án" 2
Add-Paragraph "Phần còn lại của báo cáo được tổ chức thành năm chương chính. Chương 2 trình bày khảo sát hiện trạng và phân tích yêu cầu của hệ thống game, bao gồm các nhóm chức năng, tác nhân, use case và yêu cầu phi chức năng. Chương 3 giới thiệu nền tảng lý thuyết và công nghệ sử dụng trong đồ án, tập trung vào Unity, C#, ScriptableObject, mô hình game loop, cơ chế battle theo lượt và lưu dữ liệu. Chương 4 trình bày thiết kế, triển khai và đánh giá hệ thống, bao gồm kiến trúc tổng quan, thiết kế lớp, thiết kế dữ liệu, kết quả xây dựng và kiểm thử. Chương 5 phân tích các giải pháp và đóng góp nổi bật của đồ án, nhấn mạnh các quyết định thiết kế có giá trị mở rộng. Chương 6 tổng kết kết quả đạt được, chỉ ra hạn chế và đề xuất hướng phát triển tiếp theo."
Add-Paragraph "Kết chương, chương này đã xác định bài toán của đề tài là xây dựng một game nhập vai 2D có nhiều module gameplay liên kết với nhau. Trên cơ sở đó, chương tiếp theo sẽ phân tích yêu cầu chi tiết để làm rõ các chức năng mà hệ thống cần cung cấp."

Add-Heading "CHƯƠNG 2. KHẢO SÁT VÀ PHÂN TÍCH YÊU CẦU" 1
Add-Paragraph "Chương này phân tích yêu cầu của hệ thống từ góc nhìn người chơi và người phát triển nội dung. Nội dung chương không đi sâu vào mã nguồn, mà tập trung xác định hệ thống cần làm gì, các tác nhân sử dụng là ai, các luồng chức năng chính diễn ra như thế nào và những ràng buộc phi chức năng cần đạt được."

Add-Heading "2.1 Khảo sát hiện trạng" 2
Add-Paragraph "Các game nhập vai 2D cổ điển thường tổ chức gameplay thành ba lớp trải nghiệm chính: khám phá bản đồ, tương tác với nhân vật hoặc vật thể, và xử lý các sự kiện gameplay như chiến đấu, nhận nhiệm vụ hoặc nhận thưởng. Cấu trúc này có ưu điểm là dễ hiểu với người chơi, đồng thời tạo điều kiện cho người phát triển mở rộng nội dung theo từng khu vực. Tuy nhiên, nếu không có kiến trúc phần mềm rõ ràng, các sự kiện bản đồ có thể trở nên rời rạc, khó lưu trạng thái và khó tái sử dụng."
Add-Paragraph "Với nhóm game lấy cảm hứng từ Pokemon, hệ thống chiến đấu theo lượt là thành phần trung tâm. Mỗi Pokemon có dữ liệu cơ bản như chỉ số, hệ, sprite, danh sách chiêu thức có thể học và quy tắc tiến hóa. Khi vào trận, hệ thống cần tạo đơn vị chiến đấu từ dữ liệu đó, hiển thị HUD, nhận lựa chọn của người chơi, tính sát thương, kiểm tra trạng thái, xử lý thắng thua, cộng kinh nghiệm và quay lại bản đồ. Vì vậy, battle system phải có khả năng giao tiếp với party, inventory, quest, Pokedex và save/load."
Add-Paragraph "Một đặc điểm khác của game nhập vai là tiến trình chơi thường kéo dài qua nhiều phiên. Người chơi có thể rời game ở một scene, sau đó quay lại với vị trí, đội hình, vật phẩm, nhiệm vụ và trạng thái cốt truyện như trước. Do đó, save/load không thể chỉ lưu vị trí nhân vật mà cần lưu cả dữ liệu gameplay liên quan. Trong project này, save data đã bao gồm party, storage, tiền, scene, vị trí, quest snapshot, story flag, Pokedex, NPC state, inventory, trigger đã chạy và Pokemon ngoài map đã bắt."
Add-Paragraph "Từ khảo sát trên, hệ thống cần được thiết kế theo hướng có trung tâm điều phối trạng thái, các module chuyên trách và dữ liệu có thể cấu hình. Cách làm này giúp game duy trì tính nhất quán khi người chơi chuyển giữa bản đồ, menu, dialog và battle."

Add-Heading "2.2 Tổng quan chức năng" 2
Add-Paragraph "Hệ thống có một tác nhân chính là người chơi. Ngoài ra, dưới góc nhìn phát triển nội dung, có thể xem nhà thiết kế game là tác nhân phụ vì người này cấu hình Pokemon, move, item, quest, story step, encounter zone và các trigger trong Unity Editor. Hai tác nhân này có nhu cầu khác nhau: người chơi cần trải nghiệm ổn định, dễ hiểu; còn người thiết kế cần công cụ cấu hình đủ linh hoạt để mở rộng nội dung."
Add-Table @("Nhóm chức năng", "Mô tả") @(
    @("Khám phá bản đồ", "Người chơi di chuyển theo lưới, chuyển scene, tương tác với NPC, chest, cửa, cỏ, khu vực gặp Pokemon và Pokemon ngoài map."),
    @("Hội thoại và NPC", "NPC có thể nói chuyện, mở shop, cho nhiệm vụ, bắt đầu trainer battle hoặc phản hồi theo trạng thái nhiệm vụ."),
    @("Chiến đấu", "Hệ thống hỗ trợ wild battle và trainer battle, chọn move, dùng item, đổi Pokemon, chạy trốn khi được phép, tính damage và trạng thái."),
    @("Thu phục và quản lý Pokemon", "Người chơi có thể bắt Pokemon bằng Pokeball, thêm vào party hoặc storage, xem Pokedex và theo dõi Pokemon đã thấy hoặc đã bắt."),
    @("Nhiệm vụ và cốt truyện", "QuestManager xử lý objective theo event, MainStoryDirector chạy step/action và StoryFlags ghi nhận mốc truyện."),
    @("Vật phẩm và shop", "Inventory quản lý tiền, item, thuốc hồi phục, Pokeball, badge, experience bottle và tương tác shop."),
    @("Lưu và tải", "SaveLoadSystem ghi lại dữ liệu gameplay để người chơi tiếp tục tiến trình ở lần chơi sau.")
) "Bảng 2.1. Danh sách nhóm chức năng chính của hệ thống."

Add-Heading "2.2.1 Biểu đồ use case tổng quát" 3
Add-Paragraph "Use case tổng quát có thể mô tả bằng các nhóm hành vi chính: khám phá thế giới, tương tác NPC, tham gia chiến đấu, quản lý Pokemon, quản lý vật phẩm, thực hiện nhiệm vụ, theo dõi Pokedex và lưu/tải trò chơi. Tác nhân người chơi khởi tạo hầu hết use case thông qua input trực tiếp như di chuyển, bấm phím tương tác, chọn menu hoặc chọn hành động trong battle. Tác nhân nhà thiết kế game tác động gián tiếp bằng cách cấu hình dữ liệu trong Inspector và Resources."
Add-Placeholder "Vẽ Hình 2.1: Use case tổng quát. Các use case nên gồm Explore Map, Interact, Battle, Capture Pokemon, Manage Party, Use Item, Save/Load, Progress Quest, Configure Content."

Add-Heading "2.2.2 Phân rã use case chiến đấu" 3
Add-Paragraph "Use case chiến đấu được phân rã thành các hành vi nhỏ hơn gồm bắt đầu trận đấu, hiển thị Pokemon hai bên, chọn hành động, chọn chiêu thức, tính sát thương, xử lý status, dùng vật phẩm, đổi Pokemon, kiểm tra faint, cộng kinh nghiệm, học chiêu, tiến hóa, trao thưởng và kết thúc trận đấu. Với wild battle, người chơi có thể dùng Pokeball để bắt Pokemon. Với trainer battle, hệ thống không cho bắt Pokemon của trainer và cần xử lý nhiều Pokemon đối thủ liên tiếp."

Add-Heading "2.2.3 Phân rã use case nhiệm vụ và cốt truyện" 3
Add-Paragraph "Use case nhiệm vụ và cốt truyện gồm nhận quest, submit event, cập nhật objective, chuyển quest sang trạng thái sẵn sàng trả, trả quest, nhận thưởng và mở khóa quest tiếp theo. Đối với main story, hệ thống còn có step và action. Một step có thể yêu cầu scene, trigger ID, prologue done hoặc story flag cụ thể. Action trong step có thể là hiện hội thoại, di chuyển NPC, bắt đầu battle, cho item, cho Pokemon, hiển thị lựa chọn hoặc set story flag."

Add-Heading "2.2.4 Quy trình nghiệp vụ gameplay chính" 3
Add-Paragraph "Quy trình gameplay chính bắt đầu khi người chơi vào scene overworld. GameController đặt trạng thái là Overworld và cho phép PlayerController nhận input di chuyển. Khi người chơi bước vào cỏ hoặc tương tác với Pokemon ngoài map, hệ thống có thể tạo Pokemon hoang dã và chuyển sang BattleScene. Sau khi battle kết thúc, GameController khôi phục scene overworld, cập nhật kết quả trận, xử lý quest event, lưu trạng thái liên quan và trả quyền điều khiển cho người chơi. Quy trình này lặp lại trong suốt quá trình chơi, đồng thời được xen kẽ bởi dialog, menu, shop, storage và story cutscene."

Add-Heading "2.3 Đặc tả chức năng" 2
Add-Heading "2.3.1 Use case: Di chuyển và tương tác trên bản đồ" 3
Add-Paragraph "Tên use case là di chuyển và tương tác trên bản đồ. Tác nhân chính là người chơi. Tiền điều kiện là game đang ở trạng thái Overworld, PlayerController đang hoạt động và không có dialog hoặc battle chặn input. Luồng chính gồm người chơi nhấn phím điều hướng, hệ thống xác định hướng di chuyển, kiểm tra ô đích có vật cản hoặc interactable hay không, thực hiện di chuyển tới ô đích, cập nhật animation và sau khi di chuyển xong kiểm tra GrassTrigger. Khi người chơi nhấn phím tương tác, hệ thống lấy hướng đang nhìn, kiểm tra collider ở ô phía trước và gọi Interact nếu đối tượng có triển khai interface Interactable. Hậu điều kiện là nhân vật đứng ở vị trí mới hoặc một tương tác đã được kích hoạt."

Add-Heading "2.3.2 Use case: Gặp Pokemon hoang dã trong cỏ" 3
Add-Paragraph "Tiền điều kiện là người chơi đang ở trong vùng GrassTrigger và vùng này có EncounterZone hợp lệ. Sau mỗi bước di chuyển, GrassTrigger gọi TryEncounter. Hệ thống lấy tỉ lệ battle rate, sinh số ngẫu nhiên và nếu đạt điều kiện thì lấy một Pokemon từ danh sách WildPokemon theo encounterRate. Pokemon được tạo với level ngẫu nhiên trong khoảng minLevel và maxLevel. Sau đó GameController bắt đầu wild battle, load BattleScene, ẩn scene overworld và truyền Pokemon hoang dã cho BattleSystem. Hậu điều kiện là game chuyển sang trạng thái Battle hoặc không có trận đấu nếu điều kiện ngẫu nhiên không đạt."

Add-Heading "2.3.3 Use case: Chiến đấu theo lượt" 3
Add-Paragraph "Tiền điều kiện là BattleScene đã được load và BattleSystem đã nhận Pokemon của người chơi cùng Pokemon đối thủ. Luồng chính bắt đầu bằng SetupBattle, hiển thị dialog xuất hiện của đối thủ và Pokemon người chơi. Hệ thống so sánh tốc độ để quyết định bên đi trước. Trong lượt của người chơi, hệ thống hiển thị action menu gồm Fight, Pokemon, Item và Run. Nếu chọn Fight, người chơi chọn move, hệ thống kiểm tra accuracy, trừ PP, tính damage theo level, power, attack, defense và type effectiveness, sau đó cập nhật HP. Nếu Pokemon đối thủ faint, hệ thống cộng EXP, kiểm tra học move, kiểm tra trainer còn Pokemon hay không và kết thúc battle khi đủ điều kiện. Hậu điều kiện là battle tiếp tục, Pokemon mới được đưa ra, hoặc GameController kết thúc battle."

Add-Heading "2.3.4 Use case: Bắt Pokemon" 3
Add-Paragraph "Tiền điều kiện là người chơi đang trong wild battle, inventory có Pokeball và target là Pokemon hoang dã. Khi người chơi dùng Pokeball, BattleItemHandler trừ item nếu consumable, tính xác suất bắt dựa trên HP còn lại của target và hệ số catchRateMultiplier của ball. Nếu bắt thành công, GameController nhận Pokemon, clone Pokemon thành bản sở hữu của người chơi, thêm vào party nếu còn chỗ hoặc gửi vào storage nếu party đầy. Hệ thống đồng thời gửi event PokemonCaught và PokemonOwned cho QuestManager, cập nhật Pokedex gián tiếp thông qua party hoặc storage, và kết thúc battle với outcome Capture. Nếu bắt thất bại, target quay lại trạng thái battle và trận đấu tiếp tục."

Add-Heading "2.3.5 Use case: Thực hiện nhiệm vụ cốt truyện" 3
Add-Paragraph "Tiền điều kiện là QuestManager và MainStoryDirector đã được khởi tạo, MainStorySequence có step hợp lệ và người chơi đạt điều kiện trigger. Khi MainStoryTrigger được kích hoạt, director kiểm tra triggerId, sceneName, prologue requirement, story flag requirement và chỉ số step hiện tại. Nếu hợp lệ, director chạy lần lượt các action trong step. Action có thể hiển thị dialog, accept quest, submit quest event, di chuyển NPC, set story flag, cho Pokemon, cho item hoặc bắt đầu battle. Sau khi step hoàn thành, director cập nhật StoryFlags.MainStoryStepIndex để lưu tiến độ. Hậu điều kiện là cốt truyện tiến sang step tiếp theo hoặc dừng chờ điều kiện mới."

Add-Heading "2.3.6 Use case: Lưu và tải trò chơi" 3
Add-Paragraph "Tiền điều kiện khi lưu là SaveLoadSystem tồn tại và các manager chính có thể truy cập. Khi người chơi chọn save, hệ thống tạo SaveData chứa partyPokemons, storagePokemons, money, sceneName, vị trí player, questSnapshot, story flags, pokedex, npcStates, inventoryItems, triggeredTriggers và capturedOverworldPokemonIds. Khi load, hệ thống đọc file JSON, khôi phục party, storage, inventory, story flag, quest snapshot, Pokedex, NPC state, trigger đã dùng và Pokemon ngoài map đã bắt. Hậu điều kiện là game quay về trạng thái gần nhất được lưu, hạn chế việc người chơi phải chơi lại từ đầu."

Add-Heading "2.4 Yêu cầu phi chức năng" 2
Add-Table @("Yêu cầu", "Mô tả") @(
    @("Tính dễ mở rộng", "Có thể bổ sung Pokemon, move, item, quest và story step bằng asset cấu hình, hạn chế sửa logic lõi."),
    @("Tính ổn định", "Các state như Overworld, Battle, Dialog, Menu phải được kiểm soát để tránh input chồng chéo."),
    @("Tính nhất quán dữ liệu", "Save/load cần khôi phục đầy đủ trạng thái gameplay quan trọng, không chỉ vị trí nhân vật."),
    @("Tính dễ bảo trì", "Mỗi module có trách nhiệm rõ ràng, ví dụ battle không trực tiếp xử lý toàn bộ save và quest."),
    @("Hiệu năng", "Game 2D cần chạy mượt trong scene overworld và battle; dữ liệu Resources được load hợp lý."),
    @("Tính dễ dùng", "Input và menu cần nhất quán: di chuyển, tương tác, chọn action, quay lại và mở menu.")
) "Bảng 2.2. Yêu cầu phi chức năng của hệ thống."
Add-Paragraph "Kết chương, hệ thống cần đáp ứng cả yêu cầu gameplay và yêu cầu kỹ thuật. Những yêu cầu này là cơ sở để lựa chọn công nghệ và thiết kế kiến trúc trong các chương tiếp theo."

Add-Heading "CHƯƠNG 3. NỀN TẢNG LÝ THUYẾT VÀ CÔNG NGHỆ SỬ DỤNG" 1
Add-Paragraph "Chương này trình bày các công nghệ, mô hình và cơ sở lý thuyết được sử dụng để xây dựng hệ thống. Nội dung tập trung vào những thành phần có liên quan trực tiếp tới yêu cầu đã phân tích ở Chương 2."

Add-Heading "3.1 Unity và mô hình phát triển game 2D" 2
Add-Paragraph "Unity là game engine hỗ trợ phát triển game 2D và 3D, cung cấp hệ thống scene, GameObject, Component, physics, animation, UI, audio và build đa nền tảng. Trong đồ án này, Unity được dùng để tổ chức bản đồ thành các scene, gắn script C# vào GameObject, cấu hình collider cho tương tác, hiển thị UI bằng UGUI và quản lý tài nguyên bằng thư mục Resources."
Add-Paragraph "Mô hình Component của Unity phù hợp với game 2D vì mỗi đối tượng có thể được mở rộng bằng nhiều thành phần độc lập. Ví dụ, một NPC có thể có SpriteRenderer, Collider2D, Animator, NPC script và dữ liệu TrainerParty. Một chest có thể triển khai Interactable để người chơi tương tác, đồng thời có SpriteRenderer và BoxCollider2D để hiển thị và chặn đường. Cách tổ chức này giúp logic gắn gần với đối tượng trong scene nhưng vẫn có thể tái sử dụng qua prefab."

Add-Heading "3.2 C# và lập trình hướng đối tượng" 2
Add-Paragraph "C# là ngôn ngữ chính của Unity và được dùng để xây dựng toàn bộ logic gameplay của đồ án. Các tính chất của lập trình hướng đối tượng được sử dụng trực tiếp trong thiết kế hệ thống. Lớp Pokemon biểu diễn trạng thái runtime của một Pokemon cụ thể, trong khi PokemonBase biểu diễn dữ liệu định nghĩa dùng chung. Interface Interactable cho phép nhiều loại đối tượng khác nhau như NPC, chest, fishing spot và overworld Pokemon cùng phản hồi với thao tác tương tác của người chơi."
Add-Paragraph "Việc tách lớp dữ liệu định nghĩa và lớp trạng thái runtime là một quyết định quan trọng. PokemonBase không nên chứa HP hiện tại hoặc EXP của từng cá thể, vì nhiều Pokemon cùng loài có thể có level và trạng thái khác nhau. Ngược lại, lớp Pokemon cần giữ dữ liệu thay đổi theo quá trình chơi như level, current HP, move hiện có, EXP, friendship và pending move learn."

Add-Heading "3.3 ScriptableObject và thiết kế data-driven" 2
Add-Paragraph "ScriptableObject là cơ chế lưu dữ liệu dạng asset của Unity. Trong đồ án, PokemonBase, MoveBase, ItemBase và Quest đều là ScriptableObject. Cách làm này giúp tách dữ liệu tĩnh khỏi mã nguồn, cho phép người phát triển chỉnh sửa thông tin Pokemon, move, item và quest trong Inspector. Điều này đặc biệt cần thiết với game Pokemon-like vì số lượng dữ liệu nội dung lớn hơn rất nhiều so với số lượng thuật toán xử lý."
Add-Paragraph "Project hiện có khoảng $pokemonAssets Pokemon asset, $moveAssets move asset và $itemAssets item asset trong thư mục Resources. Nếu các dữ liệu này được hardcode, việc bảo trì sẽ khó khăn. Khi dùng ScriptableObject, một move có thể có tên, hệ, category, power, accuracy, PP, drain ratio, status effect và stat boost. BattleSystem chỉ cần đọc các thuộc tính này để thực thi luật chiến đấu tương ứng."

Add-Heading "3.4 Game loop và state machine" 2
Add-Paragraph "Game loop là vòng lặp xử lý liên tục của game, trong đó input, cập nhật trạng thái và render được thực hiện qua từng frame. Với project này, game loop không chỉ là Update của từng script, mà còn được kiểm soát bằng GameState. GameController có các trạng thái như Overworld, Battle, Dialog, Menu, NPCInteraction, Shop, Storage, Cutscene, HealingCenter và Quest. Mỗi trạng thái quyết định module nào được nhận input."
Add-Paragraph "Việc quản lý state giúp giảm lỗi input chồng chéo. Khi đang Battle, PlayerController không xử lý di chuyển overworld. Khi đang Dialog hoặc Menu, thao tác phím được chuyển cho hệ thống tương ứng. Đây là cách tổ chức cần thiết trong game có nhiều chế độ tương tác."

Add-Heading "3.5 Chiến đấu theo lượt và công thức sát thương" 2
Add-Paragraph "Battle theo lượt là mô hình trong đó mỗi bên lần lượt chọn hành động. Trong project, BattleSystem quản lý BattleState như Start, PlayerActionSelection, PlayerMoveSelection, PlayerItemSelection, PlayerPokemonSelection, NewMoveSelection, EnemyMove, Busy và BattleOver. Mỗi trạng thái phản ánh một giai đoạn cụ thể trong trận đấu."
Add-Paragraph "Công thức sát thương sử dụng các yếu tố level, power, attack, defense và hệ số hiệu quả hệ. Với move vật lý, hệ thống dùng Attack của attacker và Defense của defender; với move special, hệ thống dùng SpAttack và SpDefense. Sau đó sát thương được nhân với TypeChart.GetEffectiveness để phản ánh quan hệ hệ Pokemon. Hệ thống cũng có xác suất critical hit và hỗ trợ move có drain ratio để hồi HP theo sát thương thực tế gây ra."

Add-Heading "3.6 Lưu dữ liệu bằng JSON" 2
Add-Paragraph "Save/load trong đồ án sử dụng cấu trúc SaveData và JsonUtility để tuần tự hóa dữ liệu. Dữ liệu được lưu không phải là toàn bộ object Unity, mà là các giá trị cần thiết để khôi phục trạng thái game như tên Pokemon, level, HP, move, tiền, scene, vị trí, cờ truyện và tiến độ nhiệm vụ. Đây là cách phù hợp vì GameObject trong Unity không nên được lưu trực tiếp vào file save."
Add-Paragraph "Một điểm đáng chú ý là hệ thống lưu cả triggeredTriggers và capturedOverworldPokemonIds. Nhờ đó, các sự kiện one-shot hoặc Pokemon ngoài map đã bắt không xuất hiện lại sau khi load. Đây là yêu cầu quan trọng để tiến trình chơi có tính liên tục."

Add-Heading "3.7 Công cụ sử dụng" 2
Add-Table @("Mục đích", "Công cụ/Công nghệ", "Phiên bản/ghi chú") @(
    @("Game engine", "Unity", $unityVersion),
    @("Ngôn ngữ lập trình", "C#", "Dùng trong script Unity"),
    @("Quản lý UI", "Unity UGUI, TextMeshPro", "Theo manifest project"),
    @("Quản lý dữ liệu", "ScriptableObject, Resources", "Pokemon, Move, Item, Quest"),
    @("Lưu dữ liệu", "JsonUtility", "Serialize SaveData"),
    @("Quản lý mã nguồn", "Git", "Dùng trong workspace project"),
    @("IDE", "Visual Studio / Rider / VS Code", "Có package hỗ trợ trong manifest")
) "Bảng 3.1. Công nghệ và công cụ sử dụng."
Add-Paragraph "Kết chương, các công nghệ được lựa chọn phù hợp với yêu cầu của game 2D có nhiều dữ liệu nội dung. Chương tiếp theo sẽ trình bày cách các công nghệ này được áp dụng vào thiết kế và triển khai hệ thống cụ thể."

Add-Heading "CHƯƠNG 4. PHÂN TÍCH THIẾT KẾ, TRIỂN KHAI VÀ ĐÁNH GIÁ HỆ THỐNG" 1
Add-Paragraph "Chương này trình bày thiết kế kiến trúc, thiết kế chi tiết, kết quả triển khai và kiểm thử hệ thống. Nội dung dựa trên cấu trúc mã nguồn hiện tại của project, bao gồm $($csFiles.Count) file C# với khoảng $codeLines dòng mã và $($sceneNames.Count) scene Unity."

Add-Heading "4.1 Thiết kế kiến trúc" 2
Add-Heading "4.1.1 Lựa chọn kiến trúc phần mềm" 3
Add-Paragraph "Hệ thống được thiết kế theo hướng kiến trúc module kết hợp mô hình điều phối trung tâm. GameController đóng vai trò điều phối trạng thái và chuyển đổi giữa các module lớn. Các module như Battle, Quest, Menu, Overworld, Player và Pokemon có trách nhiệm riêng. Cách tiếp cận này không phải MVC thuần túy, nhưng có sự phân tách tương tự giữa dữ liệu, xử lý và hiển thị. Dữ liệu tĩnh nằm ở ScriptableObject, dữ liệu runtime nằm ở model, xử lý nằm ở manager/system, còn hiển thị nằm ở UI component."
Add-Paragraph "Lý do không sử dụng MVC thuần túy là Unity đã có mô hình GameObject-Component riêng. Nếu ép toàn bộ hệ thống vào MVC, code có thể trở nên xa lạ với workflow của Unity. Thay vào đó, đồ án tận dụng component của Unity nhưng vẫn giữ nguyên tắc tách trách nhiệm: BattleSystem không nên chứa dữ liệu item gốc, QuestManager không nên trực tiếp điều khiển animation battle, SaveLoadSystem không nên là nơi xử lý luật chiến đấu."

Add-Heading "4.1.2 Thiết kế tổng quan" 3
Add-Paragraph "Cấu trúc thư mục script hiện gồm các module chính: Battle, Core, GamePlay, Menu, Overworld, Player và Pokemon. Module Pokemon định nghĩa dữ liệu và trạng thái Pokemon, move, item, type và status. Module Player xử lý điều khiển nhân vật, NPC và tương tác. Module Overworld xử lý đối tượng bản đồ như grass, encounter zone, chest, door transition và overworld Pokemon. Module Battle xử lý toàn bộ luồng trận đấu. Module GamePlay chứa GameController, DialogManager, Quest, Pokedex, Storage và các hệ thống điều phối. Module Menu chứa UI túi đồ, party, shop, save/load và Pokedex menu."
Add-Table @("Module", "Số file C#", "Vai trò") @(
    @("Battle", "15", "Xử lý battle scene, battle state, battle UI, item trong battle, party trong battle và HUD."),
    @("Core", "3", "Các tiện ích lõi dùng chung."),
    @("GamePlay", "36", "Điều phối game, dialog, quest, story, Pokedex, music, storage và notification."),
    @("Menu", "24", "Menu chính, inventory, shop, party UI, save/load, Pokedex UI."),
    @("Overworld", "10", "Trigger bản đồ, encounter zone, chest, door, scene gate, blocker và Pokemon ngoài map."),
    @("Player", "8", "Điều khiển người chơi, NPC, trainer party, input tương tác."),
    @("Pokemon", "11", "Model Pokemon, base data, move, item, type chart và status.")
) "Bảng 4.1. Thống kê module mã nguồn."

Add-Heading "4.1.3 Thiết kế scene" 3
Add-Paragraph "Project hiện có các scene: $($sceneNames -join ', '). Trong đó BattleScene là scene chuyên biệt cho trận đấu, MainMenuScreen là scene menu chính, Intro và Prologue phục vụ phần mở đầu, các scene Town/Road/Gym/Cave/Mountain tạo thành thế giới overworld. Việc tách BattleScene khỏi overworld giúp giao diện và camera battle độc lập với bản đồ, đồng thời GameController có thể load/unload battle scene khi cần."
Add-Paragraph "Khi bắt đầu battle, GameController lưu tên scene overworld hiện tại, đặt state sang Battle, load BattleScene, bind BattleSystem và BattleTransition, ẩn scene overworld, cô lập camera battle và gọi StartWildBattle hoặc StartTrainerBattle. Khi battle kết thúc, GameController unload BattleScene, khôi phục overworld, đặt state về Overworld và xử lý các tác vụ sau battle như tiến hóa hoặc cập nhật NPC."

Add-Heading "4.2 Thiết kế chi tiết" 2
Add-Heading "4.2.1 Thiết kế lớp GameController" 3
Add-Paragraph "GameController là lớp điều phối cấp cao của gameplay. Lớp này giữ trạng thái GameState hiện tại, tham chiếu tới PlayerController, MenuController, BattleSystem, BattleTransition và các thông tin runtime như cachedOverworldSceneName, battleSceneLoaded, pendingBattleAllowRun, activeOverworldPokemon và LastBattleOutcome. Các phương thức quan trọng gồm StartWildBattle, StartTrainerBattle, StartOverworldPokemonBattle, EndBattle, TryReceivePokemon, LoadSceneWithFade và SetState."
Add-Paragraph "Vai trò chính của GameController là đảm bảo các module không tự chuyển state một cách tùy tiện. Khi battle bắt đầu, PlayerController không trực tiếp load scene battle mà gọi GameController. Khi battle kết thúc, BattleSystem không tự khôi phục overworld mà gọi GameController.EndBattle. Nhờ đó, các thao tác chuyển cảnh và cập nhật state được tập trung tại một nơi."

Add-Heading "4.2.2 Thiết kế lớp Pokemon và PokemonBase" 3
Add-Paragraph "PokemonBase là ScriptableObject chứa dữ liệu định nghĩa của một loài Pokemon, gồm số thứ tự, tên, mô tả, sprite trước/sau, type1, type2, chỉ số cơ bản, thông tin tiến hóa và danh sách learnable moves. Pokemon là lớp runtime đại diện cho một cá thể Pokemon cụ thể, gồm base data, level, current HP, EXP, battle participation, friendship, danh sách move hiện có, status và các chỉ số hiện tại trong battle."
Add-Paragraph "Cách tách này giúp nhiều cá thể cùng loài có thể dùng chung PokemonBase nhưng có trạng thái riêng. Ví dụ hai Bulbasaur có thể cùng dùng một asset bulbasaur.asset nhưng khác level, HP, EXP và move đang học. Pokemon cũng chịu trách nhiệm xử lý GainExp, LevelUp, TryLearnMove, ResolvePendingMoveLearn, CanEvolveNow, TryEvolve, Heal, TakeDamage và GetSaveData."

Add-Heading "4.2.3 Thiết kế lớp BattleSystem" 3
Add-Paragraph "BattleSystem quản lý trạng thái trận đấu bằng BattleState. Lớp này nhận tham chiếu tới playerUnit, enemyUnit, battle UI, dialog box, item menu, party menu và move learn UI. Khi bắt đầu wild battle hoặc trainer battle, BattleSystem setup Pokemon hai bên, bind handler học chiêu, khởi tạo item handler và party handler, sau đó chạy coroutine SetupBattle."
Add-Paragraph "Trong lượt người chơi, BattleSystem nhận input chọn action và move. Khi move được dùng, hệ thống kiểm tra trạng thái, accuracy, trừ PP, tính damage, cập nhật HP, hiển thị critical/effectiveness, áp dụng drain nếu có và kiểm tra faint. Với trainer battle, nếu Pokemon đối thủ faint, BattleSystem gọi SendNextTrainerPokemon để đưa Pokemon tiếp theo ra sân hoặc kết thúc trận khi trainer không còn Pokemon."

Add-Heading "4.2.4 Thiết kế lớp QuestManager và MainStoryDirector" 3
Add-Paragraph "QuestManager quản lý danh sách active quest, completed quest, accepted history và ready-to-turn-in quest. Hệ thống cập nhật tiến độ nhiệm vụ qua QuestEvent. Mỗi objective có type, targetId và requiredCount. Khi SubmitEvent được gọi, QuestManager duyệt active quest, so khớp event với objective, tăng progress và đánh dấu quest sẵn sàng trả khi hoàn thành."
Add-Paragraph "MainStoryDirector bổ sung lớp điều phối cốt truyện tuyến tính. Thay vì viết từng đoạn cutscene bằng code riêng, director đọc MainStorySequence. Mỗi MainStoryStep có stepId, description, sceneName, triggerId, yêu cầu prologue, yêu cầu story flag, oneShot và danh sách action. Director kiểm tra điều kiện trước khi chạy step, thực thi action tuần tự và lưu tiến độ vào StoryFlags."

Add-Heading "4.2.5 Thiết kế dữ liệu lưu" 3
Add-Paragraph "SaveData là cấu trúc trung tâm cho hệ thống lưu. Cấu trúc này chứa partyPokemons, storagePokemons, money, sceneName, player position, questSnapshot, story flags, storyMainSequenceIndex, storyMainStepIndex, pokedex, npcStates, inventoryItems, triggeredTriggers và capturedOverworldPokemonIds. Các dữ liệu phức tạp như Pokemon được chuyển thành PokemonData để lưu tên, resource id, level, HP, EXP, move và chỉ số."
Add-Paragraph "Thiết kế save data theo dạng DTO giúp hệ thống không phụ thuộc trực tiếp vào instance GameObject trong scene. Khi load, SaveLoadSystem dùng PokemonDB, MoveDB và các manager để dựng lại trạng thái runtime từ dữ liệu đã lưu. Đây là cách làm phù hợp với Unity vì object trong scene có vòng đời phụ thuộc vào scene load/unload."

Add-Heading "4.2.6 Thiết kế giao diện" 3
Add-Paragraph "Giao diện của game gồm nhiều lớp UI theo ngữ cảnh. Trong overworld, người chơi chủ yếu nhìn bản đồ, nhân vật, NPC và thông báo. Khi hội thoại, DialogBox hiển thị nội dung và portrait nếu có. Khi vào battle, BattleDialogBox hiển thị action menu, move menu và thông báo trạng thái; BattleHud hiển thị HP, level, status; BattleItemMenu và BattlePartyMenu cho phép chọn item hoặc đổi Pokemon. Trong menu, PartyMenuUI, ItemMenuUI, ShopUI, SaveLoadMenuUI và PokemonDexMenuUI phục vụ các chức năng quản lý."
Add-Placeholder "Chèn hình ảnh giao diện overworld, battle, menu item, party, Pokedex và save/load. Mỗi hình cần có chú thích theo mẫu."

Add-Heading "4.3 Xây dựng ứng dụng" 2
Add-Heading "4.3.1 Kết quả mã nguồn và dữ liệu" 3
Add-Table @("Thành phần", "Số lượng", "Ghi chú") @(
    @("File C#", "$($csFiles.Count)", "Nằm trong Assets/script."),
    @("Dòng mã C#", "$codeLines", "Tính theo Measure-Object -Line."),
    @("Scene Unity", "$($sceneNames.Count)", "Nằm trong Assets/Scenes."),
    @("Pokemon asset", "$pokemonAssets", "Nằm trong Assets/Game/Resources/PokemonData."),
    @("Move asset", "$moveAssets", "Nằm trong Assets/Game/Resources/MoveData."),
    @("Item asset", "$itemAssets", "Nằm trong Assets/Game/Resources/Item.")
) "Bảng 4.2. Thống kê kết quả xây dựng project."
Add-Paragraph "Các số liệu trên cho thấy project không chỉ gồm một prototype nhỏ mà đã có lượng dữ liệu và mã nguồn đủ lớn để thể hiện một hệ thống gameplay nhiều thành phần. Đặc biệt, số lượng move và Pokemon asset cho thấy hướng tiếp cận data-driven đã được áp dụng thực tế."

Add-Heading "4.3.2 Minh họa chức năng chính" 3
Add-Paragraph "Chức năng khám phá bản đồ cho phép người chơi di chuyển theo từng ô, tránh vật cản, tương tác với đối tượng trước mặt và chuyển scene qua DoorTransition hoặc SceneGate. Chức năng encounter trong cỏ sử dụng GrassTrigger và EncounterZone để quyết định xác suất vào battle cũng như Pokemon xuất hiện."
Add-Paragraph "Chức năng battle hỗ trợ hai loại trận: wild battle và trainer battle. Wild battle cho phép bắt Pokemon bằng Pokeball, còn trainer battle có thể có nhiều Pokemon đối thủ và trao thưởng khi thắng. Battle cũng hỗ trợ hệ thống move với damage, accuracy, PP, status effect, stat boost và drain ratio."
Add-Paragraph "Chức năng quest và story cho phép nhận nhiệm vụ, cập nhật objective qua event, tự động accept main story quest và chạy các step cốt truyện dựa trên trigger hoặc scene load. Story flag giúp khóa/mở một số bước truyện, ví dụ sau khi vào hang, sau khi nhận starter hoặc sau khi có badge."
Add-Paragraph "Chức năng save/load cho phép lưu tiến trình nhiều mặt của game. Đây là một chức năng quan trọng vì sản phẩm có nhiều scene và nhiều dữ liệu runtime. Nếu thiếu save/load, người chơi sẽ mất tiến trình sau mỗi lần thoát game."

Add-Heading "4.4 Kiểm thử" 2
Add-Paragraph "Kiểm thử trong đồ án tập trung vào các luồng gameplay có nhiều module phối hợp. Các ca kiểm thử được thiết kế theo hướng black-box ở mức người chơi, kết hợp kiểm tra log và trạng thái trong Unity Inspector khi cần. Do game có yếu tố ngẫu nhiên như encounter và catch rate, một số ca kiểm thử cần thực hiện nhiều lần hoặc tạm chỉnh tỉ lệ để dễ tái hiện."
Add-Table @("Mã", "Chức năng", "Kịch bản kiểm thử", "Kết quả kỳ vọng") @(
    @("TC01", "Di chuyển", "Người chơi di chuyển tới ô trống, ô có vật cản và ô trước NPC.", "Di chuyển thành công ở ô trống, bị chặn bởi vật cản, tương tác được với NPC khi bấm Z."),
    @("TC02", "Wild encounter", "Đặt battleRatePercent cao, đi trong grass zone có EncounterZone.", "BattleScene được load, Pokemon hoang dã xuất hiện đúng level cấu hình."),
    @("TC03", "Bắt Pokemon", "Trong wild battle dùng Pokeball khi inventory còn item.", "Nếu bắt thành công, Pokemon vào party/storage, battle kết thúc, quest event được gửi."),
    @("TC04", "Trainer battle", "Đánh NPC trainer có nhiều Pokemon.", "Trainer đưa Pokemon kế tiếp ra sân, chỉ kết thúc khi hết Pokemon."),
    @("TC05", "Quest event", "Submit event đúng targetId của objective.", "Progress tăng, quest chuyển ready-to-turn-in hoặc completed theo cấu hình."),
    @("TC06", "Story flag", "Kích hoạt step yêu cầu flag chưa đạt và sau đó set flag.", "Step không chạy khi thiếu flag và chạy khi flag đạt điều kiện."),
    @("TC07", "Save/load", "Lưu sau khi bắt Pokemon ngoài map, thoát và load lại.", "Pokemon ngoài map đã bắt không xuất hiện lại, party/storage và story flags được khôi phục.")
) "Bảng 4.3. Một số ca kiểm thử chức năng chính."
Add-Placeholder "Sau khi test trực tiếp trong Unity, bổ sung cột trạng thái Đạt/Không đạt, ảnh minh họa và mô tả lỗi nếu có."

Add-Heading "4.5 Triển khai" 2
Add-Paragraph "Trong phạm vi đồ án, hệ thống được triển khai và chạy thử trong Unity Editor trên máy phát triển cá nhân. Project sử dụng scene-based workflow của Unity, trong đó các scene overworld, battle và menu được lưu trong Assets/Scenes. BattleScene được load động khi cần chiến đấu, còn các scene overworld được chuyển bằng cơ chế scene gate hoặc door transition."
Add-Paragraph "Để triển khai bản chơi thử, project có thể được build ra Windows executable thông qua Build Settings của Unity. Các scene cần đưa vào Build Settings gồm MainMenuScreen, Intro, Prologue, các scene bản đồ chính và BattleScene. Sau khi build, cần kiểm tra lại đường dẫn Resources, save path, input, âm thanh và tỷ lệ hiển thị UI."
Add-Placeholder "Bổ sung thông tin cấu hình máy test, hệ điều hành, độ phân giải, FPS trung bình và ảnh bản build nếu có."
Add-Paragraph "Kết chương, hệ thống đã được thiết kế và triển khai theo kiến trúc module, có dữ liệu nội dung đáng kể và có các luồng gameplay chính. Chương tiếp theo sẽ phân tích sâu hơn các giải pháp nổi bật có giá trị đóng góp của đồ án."

Add-Heading "CHƯƠNG 5. CÁC GIẢI PHÁP VÀ ĐÓNG GÓP NỔI BẬT" 1
Add-Paragraph "Chương này tập trung vào các quyết định thiết kế có ý nghĩa nhất trong đồ án. Các nội dung được chọn không lặp lại toàn bộ Chương 4, mà phân tích lý do, cách thực hiện và giá trị của từng giải pháp."

Add-Heading "5.1 Thiết kế data-driven bằng ScriptableObject" 2
Add-Paragraph "Vấn đề đầu tiên của game Pokemon-like là số lượng dữ liệu lớn. Mỗi Pokemon có nhiều chỉ số, sprite, hệ, move học theo level và thông tin tiến hóa. Mỗi move lại có power, accuracy, PP, category, target, status effect, boost hoặc drain. Nếu dữ liệu này nằm trong code, mỗi lần thêm nội dung mới đều cần biên dịch lại và nguy cơ gây lỗi logic rất cao."
Add-Paragraph "Giải pháp của đồ án là đưa dữ liệu định nghĩa vào ScriptableObject. PokemonBase mô tả loài Pokemon, MoveBase mô tả chiêu thức, ItemBase mô tả vật phẩm và Quest mô tả nhiệm vụ. Runtime chỉ đọc dữ liệu từ các asset này. Ví dụ, BattleSystem không cần biết riêng Absorb hay Giga Drain là move nào; nó chỉ cần kiểm tra DrainRatio của MoveBase. Nếu một move có drainNumerator và drainDenominator hợp lệ, hệ thống sẽ hồi HP theo sát thương thực tế."
Add-Paragraph "Kết quả là dữ liệu game có thể được mở rộng dễ hơn. Project đã có hàng trăm move asset và hơn một trăm Pokemon asset. Điều này chứng minh mô hình data-driven phù hợp với bài toán. Người phát triển có thể thêm Pokemon hoặc move mới bằng cách tạo asset và cấu hình thông tin, thay vì sửa nhiều đoạn code điều kiện."

Add-Heading "5.2 Điều phối battle scene bằng GameController" 2
Add-Paragraph "Một khó khăn khi làm battle trong game RPG là chuyển đổi giữa bản đồ và màn hình chiến đấu. Nếu battle được đặt trực tiếp trong cùng scene overworld, giao diện, camera, input và object bản đồ dễ chồng chéo. Nếu chuyển scene hoàn toàn, việc quay lại đúng vị trí và trạng thái overworld cũng phức tạp."
Add-Paragraph "Đồ án chọn cách dùng BattleScene riêng và GameController điều phối việc load/unload. Khi battle bắt đầu, GameController lưu scene overworld đang active, load BattleScene, ẩn overworld và cô lập camera. Khi battle kết thúc, GameController unload BattleScene, khôi phục overworld, xử lý kết quả trận và trả state về Overworld. Cách làm này giúp battle có không gian UI riêng nhưng không làm mất trạng thái bản đồ."
Add-Paragraph "Giải pháp này cũng hỗ trợ nhiều nguồn khởi tạo battle: gặp Pokemon trong cỏ, tương tác với Pokemon ngoài map, trainer NPC hoặc action StartBattle trong main story. Các nguồn khác nhau đều đi qua GameController, nhờ đó tránh trùng lặp logic chuyển scene."

Add-Heading "5.3 Hệ thống main story sequence có điều kiện flag" 2
Add-Paragraph "Cốt truyện trong game nhập vai thường gồm nhiều bước phụ thuộc vào scene, trigger, lựa chọn trước đó và các mốc đã hoàn thành. Nếu mỗi đoạn cốt truyện đều viết thành một script riêng, hệ thống sẽ khó mở rộng và khó kiểm soát thứ tự. Đồ án giải quyết bằng MainStorySequence, MainStoryStep và MainStoryAction."
Add-Paragraph "Mỗi step có thể cấu hình sceneName, triggerId, triggerOnSceneLoad, requirePrologueDone, requireStoryFlag, requiredStoryFlag và oneShot. Nhờ đó, một step chỉ chạy khi người chơi ở đúng scene, đúng trigger và đạt điều kiện truyện. Action trong step có nhiều loại như ShowDialog, AcceptQuest, SubmitEvent, MoveNpc, SetStoryFlag, GivePokemon, ShowChoice, StartBattle, FadeNpc, GiveItem và TakeItem."
Add-Paragraph "Giá trị của giải pháp này là tách cốt truyện khỏi code. Khi muốn thêm một đoạn truyện mới sau flag InCave hoặc sau badge, người thiết kế có thể thêm step vào asset MainStorySequence và cấu hình requiredStoryFlag. Điều này phù hợp với quá trình phát triển game vì cốt truyện thường thay đổi nhiều hơn logic lõi."

Add-Heading "5.4 Quest event-driven và story flag" 2
Add-Paragraph "Quest trong game cần cập nhật từ nhiều nguồn: bắt Pokemon, sở hữu Pokemon, nói chuyện NPC, đánh bại trainer, nhận item, tới địa điểm hoặc sự kiện tùy chỉnh. Nếu mỗi nguồn gọi trực tiếp từng quest cụ thể, hệ thống sẽ phụ thuộc chặt và khó mở rộng."
Add-Paragraph "Đồ án dùng QuestEvent làm lớp trung gian. Khi một sự kiện gameplay xảy ra, hệ thống gửi QuestEvent gồm type, targetId và amount. QuestManager duyệt các active quest và tự so khớp với objective. Cách này giúp nguồn phát sự kiện không cần biết quest nào đang hoạt động. Ví dụ, khi bắt Pokemon, GameController gửi event PokemonCaught và PokemonOwned; quest liên quan sẽ tự cập nhật nếu objective phù hợp."
Add-Paragraph "Story flag bổ sung cho quest bằng cách ghi nhận mốc cốt truyện dạng boolean. Quest phù hợp với nhiệm vụ có objective và reward, trong khi story flag phù hợp với điều kiện mở khóa step, blocker, khu vực hoặc lời thoại. Việc kết hợp hai cơ chế này giúp hệ thống vừa có nhiệm vụ định lượng, vừa có cờ trạng thái tuyến tính cho cốt truyện."

Add-Heading "5.5 Save/load nhiều lớp trạng thái" 2
Add-Paragraph "Save/load là một phần khó vì game có nhiều loại trạng thái. Nếu chỉ lưu party và vị trí người chơi, nhiều sự kiện khác sẽ bị mất sau khi load. Ví dụ, chest đã mở có thể xuất hiện lại, Pokemon ngoài map đã bắt có thể hồi sinh, NPC đã bị đánh bại có thể cho battle lại, hoặc story step đã chạy có thể chạy lần nữa."
Add-Paragraph "Đồ án thiết kế SaveData gồm nhiều lớp thông tin: party, storage, tiền, scene, vị trí, quest snapshot, story flags, Pokedex, NPC state, inventory items, triggered triggers và captured overworld Pokemon IDs. Khi save, hệ thống thu thập dữ liệu từ các manager hiện có. Khi load, hệ thống áp dụng lại dữ liệu vào runtime và scene object."
Add-Paragraph "Giải pháp này tạo nền tảng cho trải nghiệm chơi liên tục. Người chơi có thể bắt Pokemon, nhận item, hoàn thành quest, chuyển scene và sau đó load lại mà không làm mất các mốc quan trọng. Đây là yêu cầu cơ bản để game vượt qua mức prototype ngắn."

Add-Heading "5.6 Overworld Pokemon có thể bắt và biến mất sau khi bắt" 2
Add-Paragraph "Ngoài random encounter trong cỏ, đồ án bổ sung cơ chế Pokemon xuất hiện trực tiếp trên bản đồ. Khi người chơi tương tác, Pokemon đó đưa người chơi vào wild battle và vẫn có thể bị bắt như Pokemon hoang dã. Nếu người chơi bắt thành công hoặc đánh bại theo logic auto capture đã cấu hình, Pokemon ngoài map sẽ biến mất và ID của nó được lưu lại."
Add-Paragraph "Giải pháp dùng lớp OverworldPokemon triển khai Interactable. Lớp này có encounterId, PokemonBase, level và allowRun. Khi Start, nó kiểm tra SaveLoadSystem để biết encounterId đã bị bắt chưa. Nếu đã bị bắt, GameObject bị ẩn. Khi interact, nó tạo Pokemon runtime và gọi GameController.StartOverworldPokemonBattle. Sau battle, nếu captured là true, nó đăng ký ID vào SaveLoadSystem và tắt GameObject."
Add-Paragraph "Cơ chế này làm thế giới game có cảm giác cụ thể hơn so với chỉ random encounter. Một Pokemon đặc biệt có thể được đặt ở vị trí cố định trong hang hoặc sau một cờ truyện, tạo thành phần thưởng khám phá hoặc sự kiện phụ."

Add-Heading "5.7 Bài học kỹ thuật rút ra" 2
Add-Paragraph "Trong quá trình phát triển, một bài học quan trọng là cần phân biệt rõ dữ liệu định nghĩa, dữ liệu runtime và dữ liệu lưu. PokemonBase, MoveBase và ItemBase là dữ liệu định nghĩa; Pokemon, Inventory và QuestRuntimeState là dữ liệu runtime; SaveData và PokemonData là dữ liệu lưu. Nếu ba nhóm này bị trộn lẫn, hệ thống sẽ khó debug và dễ lỗi khi chuyển scene."
Add-Paragraph "Bài học thứ hai là state machine cần được kiểm soát nghiêm túc. Battle, menu, dialog và overworld đều dùng input bàn phím, vì vậy nếu không có GameState và BattleState rõ ràng, cùng một phím có thể kích hoạt nhiều hành vi. Việc tập trung điều phối ở GameController và BattleSystem giúp giảm lỗi này."
Add-Paragraph "Bài học thứ ba là nội dung game nên được cấu hình nhiều nhất có thể. Cốt truyện, quest, Pokemon và move là những phần thay đổi thường xuyên. Khi chúng được đưa vào asset, quá trình thử nghiệm gameplay nhanh hơn và ít phải sửa mã nguồn hơn."

Add-Heading "CHƯƠNG 6. KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN" 1
Add-Heading "6.1 Kết luận" 2
Add-Paragraph "Đồ án đã xây dựng được một hệ thống game nhập vai 2D lấy cảm hứng từ Pokemon với nhiều chức năng cốt lõi. Người chơi có thể di chuyển trên bản đồ, tương tác với NPC và vật thể, gặp Pokemon hoang dã, tham gia battle theo lượt, sử dụng vật phẩm, bắt Pokemon, nhận thưởng, quản lý party/storage, theo dõi Pokedex, thực hiện quest và lưu/tải tiến trình. Hệ thống cũng có main story sequence, story flag và các trigger cho phép mở rộng cốt truyện theo từng bước."
Add-Paragraph "Về mặt kỹ thuật, đồ án đã áp dụng được mô hình module hóa trong Unity. Các thành phần chính như GameController, BattleSystem, QuestManager, MainStoryDirector, SaveLoadSystem, PlayerController, Inventory, StorageSystem và PokedexManager có trách nhiệm tương đối rõ. Dữ liệu game được tổ chức theo hướng data-driven bằng ScriptableObject, giúp giảm hardcode và hỗ trợ mở rộng nội dung."
Add-Paragraph "So với một demo game 2D đơn giản, sản phẩm có điểm mạnh ở mức độ liên kết hệ thống. Battle không tồn tại độc lập mà ảnh hưởng tới EXP, move learn, evolution, quest, Pokedex, badge và save data. Quest không chỉ là danh sách nhiệm vụ tĩnh mà cập nhật bằng event. Story không chỉ là hội thoại mà có step, action và flag. Save/load không chỉ lưu vị trí mà lưu nhiều lớp trạng thái gameplay."
Add-Paragraph "Tuy vậy, sản phẩm vẫn còn hạn chế. Cân bằng gameplay chưa được đánh giá sâu, AI đối thủ còn đơn giản, giao diện cần được chuẩn hóa hơn, hệ thống animation và hiệu ứng battle có thể cải thiện, một số kiểm thử vẫn cần thực hiện thủ công trong Unity Editor. Ngoài ra, báo cáo cần bổ sung hình ảnh thực tế, biểu đồ UML hoàn chỉnh và kết quả test có số liệu sau khi build bản chơi thử."

Add-Heading "6.2 Hướng phát triển" 2
Add-Paragraph "Hướng phát triển đầu tiên là hoàn thiện nội dung và cân bằng gameplay. Cần rà soát chỉ số Pokemon, power của move, tỉ lệ encounter, tỉ lệ bắt, tiền thưởng, item trong shop và độ khó trainer. Việc cân bằng nên dựa trên playtest nhiều lần thay vì chỉ dựa vào cảm nhận khi cấu hình."
Add-Paragraph "Hướng phát triển thứ hai là nâng cấp hệ thống battle. Có thể bổ sung AI chọn move theo type effectiveness, move priority, hiệu ứng thời tiết, buff/debuff nhiều cấp, item held, khả năng chạy trốn theo speed và animation riêng cho từng loại move. Ngoài ra, battle UI có thể hiển thị thông tin rõ hơn về PP, type, status và dự báo hiệu quả chiêu."
Add-Paragraph "Hướng phát triển thứ ba là mở rộng story editor. MainStorySequence hiện đã hỗ trợ nhiều action, nhưng có thể bổ sung action điều khiển camera, phát âm thanh, spawn/despawn object, chờ NPC tới vị trí, điều kiện nhiều flag cùng lúc và branch story phức tạp hơn. Nếu có editor custom mạnh hơn, người thiết kế có thể tạo cốt truyện mà ít cần can thiệp code."
Add-Paragraph "Hướng phát triển thứ tư là cải thiện save/load và kiểm thử tự động. Save data có thể được version hóa để tránh lỗi khi thay đổi cấu trúc dữ liệu. Một số module như Pokemon, QuestRuntimeState, TypeChart và damage calculation có thể viết unit test bằng Unity Test Framework. Các kịch bản smoke test sau build cũng nên được chuẩn hóa."
Add-Paragraph "Hướng phát triển cuối cùng là hoàn thiện phần trình bày sản phẩm. Cần bổ sung màn hình hướng dẫn điều khiển, menu setting, âm lượng, language option, ảnh đại diện Pokemon rõ hơn, hiệu ứng chuyển scene mượt hơn và polish giao diện. Những cải tiến này không thay đổi kiến trúc lõi nhưng giúp sản phẩm có chất lượng trải nghiệm tốt hơn."

Add-Heading "TÀI LIỆU THAM KHẢO" 1
Add-Paragraph "[1] Unity Technologies, Unity Manual. [Online]. Available: https://docs.unity3d.com/Manual/"
Add-Paragraph "[2] Unity Technologies, ScriptableObject. [Online]. Available: https://docs.unity3d.com/ScriptReference/ScriptableObject.html"
Add-Paragraph "[3] Microsoft, C# documentation. [Online]. Available: https://learn.microsoft.com/en-us/dotnet/csharp/"
Add-Paragraph "[4] Unity Technologies, Unity UI. [Online]. Available: https://docs.unity3d.com/Packages/com.unity.ugui@latest"
Add-Paragraph "[5] Unity Technologies, Unity Test Framework. [Online]. Available: https://docs.unity3d.com/Packages/com.unity.test-framework@latest"
Add-Placeholder "Bổ sung ngày truy cập và các tài liệu học thuật/sách nếu giảng viên yêu cầu theo chuẩn IEEE."

Add-Heading "PHỤ LỤC A. GỢI Ý BIỂU ĐỒ CẦN VẼ" 1
Add-Paragraph "Phụ lục này liệt kê các biểu đồ nên bổ sung vào báo cáo để tăng tính trực quan. Các biểu đồ có thể vẽ bằng draw.io, StarUML, PlantUML hoặc công cụ tương đương. Không nên dùng ảnh mờ hoặc ảnh chụp màn hình code thay cho biểu đồ thiết kế."
Add-Paragraph "Biểu đồ use case tổng quát nên có hai tác nhân là Người chơi và Nhà thiết kế nội dung. Người chơi liên kết tới các use case Khám phá bản đồ, Tương tác NPC, Chiến đấu, Bắt Pokemon, Quản lý đội hình, Sử dụng vật phẩm, Theo dõi Pokedex, Thực hiện nhiệm vụ và Lưu/Tải. Nhà thiết kế nội dung liên kết tới Cấu hình Pokemon, Cấu hình Move, Cấu hình Quest, Cấu hình MainStorySequence và Cấu hình EncounterZone."
Add-Paragraph "Biểu đồ package nên thể hiện các package Battle, Pokemon, Player, Overworld, GamePlay, Quest, Menu và SaveLoad. GamePlay phụ thuộc tới hầu hết module điều phối, Battle phụ thuộc Pokemon và Menu, Quest phụ thuộc Pokemon và Inventory khi trao thưởng, SaveLoad phụ thuộc PlayerParty, StorageSystem, Inventory, QuestManager, StoryFlags và PokedexManager."
Add-Paragraph "Biểu đồ sequence cho use case wild battle nên bắt đầu từ PlayerController sau khi di chuyển xong, gọi GrassTrigger.TryEncounter, EncounterZone.GetRandomPokemon, GameController.StartWildBattle, BattleSystem.StartWildBattle, người chơi chọn move hoặc item, BattleSystem xử lý kết quả và GameController.EndBattle khôi phục overworld."
Add-Paragraph "Biểu đồ sequence cho use case main story trigger nên bắt đầu từ MainStoryTrigger.TryTrigger, gọi MainStoryDirector.TryTrigger, kiểm tra AreStepRequirementsMet, chạy ExecuteAction cho từng action, cập nhật StoryFlags và gọi MainStoryTrigger.TryTriggerAnyOverlappingPlayerTriggers nếu cần."

Add-Heading "PHỤ LỤC B. ĐẶC TẢ BỔ SUNG MỘT SỐ LỚP" 1
Add-Paragraph "PlayerController chịu trách nhiệm nhận input trong trạng thái Overworld, xử lý di chuyển theo trục ngang/dọc, cập nhật Animator, kiểm tra vật cản bằng Physics2D.OverlapCircle, gọi Interactable.Interact và kiểm tra GrassTrigger sau mỗi bước đi."
Add-Paragraph "EncounterZone chứa danh sách WildPokemon, trong đó mỗi phần tử gồm PokemonBase, minLevel, maxLevel và encounterRate. Khi được gọi, EncounterZone chọn Pokemon theo tỉ lệ tích lũy và tạo một Pokemon runtime với level ngẫu nhiên trong khoảng cấu hình."
Add-Paragraph "Chest là đối tượng Interactable có thể trao item, tiền, Pokemon hoặc badge. Sau khi mở, chest chuyển sang trạng thái opened và lần tương tác sau chỉ hiển thị thông báo rỗng. Phiên bản hiện tại cần được mở rộng thêm ID và save state nếu muốn chest đã mở được lưu bền vững qua phiên chơi."
Add-Paragraph "OverworldPokemon là đối tượng Interactable đại diện cho Pokemon xuất hiện cố định trên bản đồ. Nó tạo battle Pokemon khi người chơi tương tác và tự ẩn sau khi bị bắt. Trạng thái đã bắt được lưu bằng capturedOverworldPokemonIds."

Add-Heading "PHỤ LỤC C. DANH SÁCH VIỆC CẦN HOÀN THIỆN TRƯỚC KHI NỘP" 1
Add-Paragraph "Cần thay toàn bộ thông tin cá nhân ở trang bìa, gồm họ tên, MSSV, email, chương trình đào tạo, khoa/trường và giảng viên hướng dẫn. Cần cập nhật tháng năm theo kỳ nộp chính thức."
Add-Paragraph "Cần chụp ảnh màn hình các chức năng chính trong Unity: main menu, overworld, dialog NPC, wild battle, trainer battle, bắt Pokemon, party menu, item menu, Pokedex, save/load và main story trigger. Mỗi hình phải có chú thích và được nhắc tới trong nội dung."
Add-Paragraph "Cần vẽ ít nhất một use case diagram, một package diagram và hai sequence diagram cho battle và story trigger. Nếu còn thời gian, nên bổ sung class diagram rút gọn cho GameController, BattleSystem, Pokemon, QuestManager, SaveLoadSystem."
Add-Paragraph "Cần chạy test thực tế theo Bảng 4.3 và ghi kết quả. Nếu có lỗi chưa sửa, nên mô tả trung thực ở phần hạn chế thay vì bỏ qua."
Add-Paragraph "Cần kiểm tra lại chính tả, thống nhất thuật ngữ Pokemon/Pokémon, thống nhất cách viết tên class, cập nhật số trang trong mục lục bằng Word và xóa các dòng [CẦN BỔ SUNG] trước khi nộp bản cuối."

$Document.TablesOfContents.Item(1).Update()
$Document.SaveAs([ref]$fullOutputPath, [ref]16)
$Document.Close()
$word.Quit()

Write-Host "Created $fullOutputPath"
