namespace SmartTyping.UI.Localization;

/// <summary>
/// The UI string table. Each entry maps a key to its (English, Thai) translations. Kept in code
/// (rather than .resx) for simple, satellite-assembly-free runtime switching between two languages.
/// </summary>
internal static class Strings
{
    public static readonly IReadOnlyDictionary<string, (string En, string Th)> Table =
        new Dictionary<string, (string En, string Th)>
        {
            // Main window
            ["Main_Tagline"] = ("Thai–English typing assistant", "ตัวช่วยพิมพ์ไทย–อังกฤษ"),
            ["Main_Categories"] = ("Categories", "หมวดหมู่"),
            ["Main_All"] = ("All", "ทั้งหมด"),
            ["Main_Add"] = ("Add", "เพิ่ม"),
            ["Main_Edit"] = ("Edit", "แก้ไข"),
            ["Main_Delete"] = ("Delete", "ลบ"),
            ["Main_Import"] = ("Import", "นำเข้า"),
            ["Main_Export"] = ("Export", "ส่งออก"),
            ["Main_Settings"] = ("Settings", "ตั้งค่า"),
            ["Main_Stats"] = ("Stats", "สถิติ"),
            ["Main_ManageCategories"] = ("Manage", "จัดการ"),
            ["Main_SearchTooltip"] = ("Search triggers and content", "ค้นหาคำสั่งและเนื้อหา"),
            ["Main_Col_On"] = ("On", "เปิด"),
            ["Main_Col_Trigger"] = ("Trigger", "คำสั่ง"),
            ["Main_Col_Content"] = ("Content", "เนื้อหา"),
            ["Main_Col_Uses"] = ("Uses", "ใช้"),
            ["Main_Empty_Title"] = ("No snippets yet", "ยังไม่มี snippet"),
            ["Main_Empty_Hint"] = (
                "Click Add to create your first snippet.\nThen select its trigger anywhere and press Ctrl+Shift+E to expand it.",
                "กด ‘เพิ่ม’ เพื่อสร้าง snippet แรก\nจากนั้นเลือกคำสั่งที่ไหนก็ได้ แล้วกด Ctrl+Shift+E เพื่อขยาย"),

            // Edit dialog
            ["Edit_Title_Add"] = ("Add Snippet", "เพิ่ม Snippet"),
            ["Edit_Title_Edit"] = ("Edit Snippet", "แก้ไข Snippet"),
            ["Edit_Trigger"] = ("Trigger:", "คำสั่ง:"),
            ["Edit_Category"] = ("Category:", "หมวดหมู่:"),
            ["Edit_Enabled"] = ("Enabled", "เปิดใช้งาน"),
            ["Edit_Content"] = ("Content:", "เนื้อหา:"),
            ["Edit_Variables"] = (
                "Variables: {date}  {date:yyyy-MM-dd}  {date+7}  {time}  {clipboard}  {cursor}  {input:Label}",
                "ตัวแปร: {date}  {date:yyyy-MM-dd}  {date+7}  {time}  {clipboard}  {cursor}  {input:ป้าย}"),
            ["Edit_Preview"] = ("Preview ▶", "ทดลอง ▶"),
            ["Edit_Cancel"] = ("Cancel", "ยกเลิก"),
            ["Edit_Save"] = ("Save", "บันทึก"),

            // Settings window
            ["Settings_Title"] = ("Settings", "ตั้งค่า"),
            ["Settings_Features"] = ("Features", "ฟีเจอร์"),
            ["Settings_ExpansionEnabled"] = ("Enable snippet expansion", "เปิดการขยาย snippet"),
            ["Settings_AutoExpand"] = ("Expand automatically as I type", "ขยายอัตโนมัติขณะพิมพ์"),
            ["Settings_AutoExpandHint"] = (
                "Finish typing a trigger (e.g. /sig) and it's replaced instantly — no hotkey, no space. Triggers that start another trigger expand on space/tab instead. Skips password fields.",
                "พิมพ์คำสั่งให้ครบ (เช่น /sig) แล้วแทนที่ทันที ไม่ต้องกดคีย์ลัด ไม่ต้องเคาะ space · คำสั่งที่เป็นส่วนต้นของคำสั่งอื่นจะรอ space/tab · ข้ามช่องรหัสผ่าน"),
            ["Settings_CorrectionEnabled"] = ("Enable language correction", "เปิดการแก้ภาษา"),
            ["Settings_AutoCorrectSuggest"] = ("Suggest layout fixes as I type", "แนะนำการแก้เลย์เอาต์ขณะพิมพ์"),
            ["Settings_AutoCorrectSuggestHint"] = (
                "Shows a tray hint when a word looks like wrong-layout text. Never replaces anything — press the convert hotkey to apply.",
                "แสดงคำแนะนำใน tray เมื่อคำที่พิมพ์ดูเหมือนพิมพ์ผิดเลย์เอาต์ ไม่แทนที่ให้เอง— กดคีย์ลัดแปลงภาษาเพื่อแก้"),
            ["Settings_AutoCorrectAuto"] = ("Fix automatically (replace as I type)", "แก้ให้อัตโนมัติ (แทนที่ขณะพิมพ์)"),
            ["Settings_AutoCorrectAutoHint"] = (
                "Fixes wrong-layout Thai the moment it's recognised — no space needed — and switches your keyboard to Thai so the rest of the word types correctly. Uses a stricter rule so English words like don't / it's are left alone.",
                "แก้ให้ทันทีที่รู้ว่าพิมพ์ผิดเลย์เอาต์ ไม่ต้องเคาะ space และสลับคีย์บอร์ดเป็นไทยให้ด้วย ตัวที่พิมพ์ต่อจะเป็นไทยเลย · ใช้เกณฑ์เข้มขึ้นเพื่อไม่แตะคำอังกฤษอย่าง don't / it's"),
            ["Settings_Hotkeys"] = ("Hotkeys", "คีย์ลัด"),
            ["Settings_ConvertLayout"] = ("Convert layout", "แปลงภาษา"),
            ["Settings_ExpandSnippet"] = ("Expand snippet", "ขยาย snippet"),
            ["Settings_Picker"] = ("Quick-picker", "ค้นหา snippet"),
            ["Settings_Capture"] = ("Add from selection", "เพิ่มจากที่เลือก"),
            ["Settings_AiImprove"] = ("AI improve selection", "ปรับข้อความด้วย AI"),
            ["Settings_Change"] = ("Change", "เปลี่ยน"),
            ["Hotkey_Prompt"] = ("Press a key combination…", "กดคีย์ผสมที่ต้องการ…"),
            ["Hotkey_Duplicate"] = ("That combination is already used by another action.", "คีย์นี้ถูกใช้กับคำสั่งอื่นแล้ว"),
            ["Settings_StartWithWindows"] = ("Start with Windows", "เปิดพร้อม Windows"),
            ["Settings_Language"] = ("Language:", "ภาษา:"),
            ["Settings_Theme"] = ("Theme:", "ธีม:"),
            ["Theme_System"] = ("System", "ตามระบบ"),
            ["Theme_Light"] = ("Light", "สว่าง"),
            ["Theme_Dark"] = ("Dark", "มืด"),
            ["Settings_OpenLogs"] = ("Open logs", "เปิด log"),
            ["Settings_Close"] = ("Close", "ปิด"),
            ["Settings_Updates"] = ("Updates", "อัปเดต"),
            ["Settings_CheckForUpdates"] = ("Check for updates", "ตรวจหาอัปเดต"),
            ["Settings_CheckNow"] = ("Check now", "ตรวจเดี๋ยวนี้"),
            ["Update_Title"] = ("Update", "อัปเดต"),
            ["Update_Checking"] = ("Checking…", "กำลังตรวจสอบ…"),
            ["Update_UpToDate"] = ("You're up to date.", "เป็นเวอร์ชันล่าสุดแล้ว"),
            ["Update_Available"] = ("Update available: v{0}", "มีอัปเดต: v{0}"),
            ["Update_AvailablePrompt"] = ("Version {0} is available. Open the download page?", "มีเวอร์ชัน {0} แล้ว เปิดหน้าดาวน์โหลดไหม?"),

            // AI (opt-in, brings your own key)
            ["Settings_Ai"] = ("AI (optional)", "AI (ไม่บังคับ)"),
            ["Settings_AiHint"] = (
                "Improve the selected text with AI. Paste a free Google Gemini API key to enable, then select text and press the AI-improve hotkey. Text is sent to Google only when you use it.",
                "ปรับข้อความที่เลือกด้วย AI วางคีย์ Google Gemini API (ฟรี) เพื่อเปิดใช้ จากนั้นเลือกข้อความแล้วกดคีย์ลัดปรับข้อความ ระบบจะส่งข้อความไป Google เฉพาะตอนที่คุณสั่งเท่านั้น"),
            ["Settings_AiApiKey"] = ("Gemini API key:", "คีย์ Gemini API:"),

            // Onboarding
            ["Onb_Title"] = ("Welcome to SmartTyping", "ยินดีต้อนรับสู่ SmartTyping"),
            ["Onb_Heading"] = ("Welcome to SmartTyping ⚡", "ยินดีต้อนรับสู่ SmartTyping ⚡"),
            ["Onb_Intro"] = (
                "Type faster and fix wrong-keyboard-layout mistakes — everything stays on your PC. Two hotkeys do the work (nothing happens automatically):",
                "พิมพ์เร็วขึ้นและแก้ข้อความผิดเลย์เอาต์ — ทุกอย่างอยู่ในเครื่องคุณ ใช้แค่ 2 คีย์ลัด (ไม่มีอะไรทำงานอัตโนมัติ):"),
            ["Onb_Fix_Title"] = ("Fix Thai ⇄ English layout", "แก้ภาษาไทย ⇄ อังกฤษ"),
            ["Onb_Fix_Body"] = (
                "Select gibberish text (e.g. l;ylfu) and press Ctrl + Shift + L → สวัสดี",
                "เลือกข้อความที่เพี้ยน (เช่น l;ylfu) แล้วกด Ctrl + Shift + L → สวัสดี"),
            ["Onb_Expand_Title"] = ("Expand a snippet", "ขยาย snippet"),
            ["Onb_Expand_Body"] = (
                "Select a trigger you created (e.g. /phone) and press Ctrl + Shift + E to replace it with your saved text.",
                "เลือกคำสั่งที่คุณสร้าง (เช่น /phone) แล้วกด Ctrl + Shift + E เพื่อแทนที่ด้วยข้อความที่บันทึกไว้"),
            ["Onb_Tray"] = (
                "SmartTyping lives in the system tray — close the window and it keeps running. Manage snippets and settings any time from the tray icon.",
                "SmartTyping อยู่ใน system tray — ปิดหน้าต่างแล้วก็ยังทำงานอยู่ เปิดหน้าจัดการและตั้งค่าได้จากไอคอนใน tray ทุกเมื่อ"),
            ["Onb_GotIt"] = ("Got it", "เข้าใจแล้ว"),

            // Quick-picker
            ["Picker_Hint"] = ("↑↓ navigate · Enter insert · Esc cancel", "↑↓ เลื่อน · Enter แทรก · Esc ยกเลิก"),

            // Placeholder input
            ["Input_Title"] = ("Fill in the details", "กรอกข้อมูล"),
            ["Input_Insert"] = ("Insert", "แทรก"),

            // Stats
            ["Stats_Title"] = ("Usage statistics", "สถิติการใช้งาน"),
            ["Stats_TotalSnippets"] = ("Snippets", "จำนวน snippet"),
            ["Stats_Enabled"] = ("{0} enabled", "เปิดใช้ {0}"),
            ["Stats_TotalExpansions"] = ("Total expansions", "ขยายทั้งหมด (ครั้ง)"),
            ["Stats_TimeSaved"] = ("Estimated time saved", "เวลาที่ประหยัด (ประมาณ)"),
            ["Stats_Minutes"] = ("{0} min", "{0} นาที"),
            ["Stats_Seconds"] = ("{0} s", "{0} วินาที"),
            ["Stats_TopUsed"] = ("Most used", "ใช้บ่อยสุด"),
            ["Stats_None"] = ("No usage yet.", "ยังไม่มีการใช้งาน"),
            ["Stats_Clear"] = ("Clear stats", "ล้างสถิติ"),
            ["Stats_ClearConfirm"] = ("Reset all usage counts and history?", "รีเซ็ตยอดการใช้และประวัติทั้งหมด?"),

            // Categories
            ["Cat_Title"] = ("Manage categories", "จัดการหมวดหมู่"),
            ["Cat_Add"] = ("Add", "เพิ่ม"),
            ["Cat_Rename"] = ("Rename", "เปลี่ยนชื่อ"),
            ["Cat_Delete"] = ("Delete", "ลบ"),
            ["Cat_NamePrompt"] = ("Category name:", "ชื่อหมวดหมู่:"),
            ["Cat_DeleteConfirm"] = ("Delete category ‘{0}’? Its snippets are kept (uncategorized).", "ลบหมวดหมู่ ‘{0}’? snippet ในหมวดจะยังอยู่ (ไม่มีหมวด)"),
            ["Cat_Duplicate"] = ("A category with that name already exists.", "มีหมวดหมู่ชื่อนี้อยู่แล้ว"),

            // Generic prompt
            ["Prompt_Ok"] = ("OK", "ตกลง"),

            // Tray
            ["Tray_Show"] = ("Show manager", "เปิดหน้าจัดการ"),
            ["Tray_OpenLogs"] = ("Open logs", "เปิด log"),
            ["Tray_Exit"] = ("Exit", "ออก"),
            ["Tray_Converted"] = ("Converted", "แปลงแล้ว"),
            ["Tray_Expanded"] = ("Expanded", "ขยายแล้ว"),
            ["Tray_Suggestion"] = ("Wrong layout?", "พิมพ์ผิดเลย์เอาต์?"),
            ["Tray_SuggestionBody"] = ("‘{0}’ → ‘{1}’ · press the convert hotkey", "‘{0}’ → ‘{1}’ · กดคีย์ลัดแปลงภาษา"),
            ["Tray_AutoFixed"] = ("Fixed layout", "แก้เลย์เอาต์แล้ว"),
            ["Tray_AutoFixedBody"] = ("‘{0}’ → ‘{1}’", "‘{0}’ → ‘{1}’"),
            ["Tray_AiImproved"] = ("AI improved", "AI ปรับข้อความแล้ว"),
            ["Tray_AiWorking_Title"] = ("AI", "AI"),
            ["Tray_AiWorking"] = ("Improving…", "กำลังปรับข้อความ…"),
            ["Tray_AiNotConfigured_Title"] = ("AI not set up", "ยังไม่ได้ตั้งค่า AI"),
            ["Tray_AiNotConfigured"] = ("Add a Gemini API key in Settings first.", "เพิ่มคีย์ Gemini API ในหน้าตั้งค่าก่อน"),

            // Status messages (formatted)
            ["Status_Loaded"] = ("{0} snippet(s) loaded.", "โหลด {0} snippet แล้ว"),
            ["Status_Added"] = ("Snippet added.", "เพิ่ม snippet แล้ว"),
            ["Status_Updated"] = ("Snippet updated.", "อัปเดต snippet แล้ว"),
            ["Status_Deleted"] = ("Snippet deleted.", "ลบ snippet แล้ว"),
            ["Status_Enabled"] = ("{0} enabled.", "เปิด {0} แล้ว"),
            ["Status_Disabled"] = ("{0} disabled.", "ปิด {0} แล้ว"),
            ["Status_Exported"] = ("Exported {0} snippet(s) to {1}.", "ส่งออก {0} snippet ไปที่ {1} แล้ว"),
            ["Status_ExportFailed"] = ("Export failed: {0}", "ส่งออกล้มเหลว: {0}"),
            ["Status_ImportDone"] = ("Import complete. {0}", "นำเข้าเสร็จ {0}"),
            ["Status_ImportFailed"] = ("Import failed: {0}", "นำเข้าล้มเหลว: {0}"),

            // Dialogs
            ["Dlg_DeleteTitle"] = ("Delete snippet", "ลบ snippet"),
            ["Dlg_DeleteMsg"] = ("Delete snippet ‘{0}’?", "ลบ snippet ‘{0}’?"),
            ["Dlg_ExportTitle"] = ("Export snippets", "ส่งออก snippet"),
            ["Dlg_ImportTitle"] = ("Import snippets", "นำเข้า snippet"),
            ["Dlg_ImportPrompt"] = (
                "Overwrite snippets whose trigger already exists?\n\nYes = overwrite,  No = keep existing.",
                "เขียนทับ snippet ที่คำสั่งซ้ำหรือไม่?\n\nใช่ = เขียนทับ,  ไม่ = เก็บของเดิม"),
            ["File_JsonFilter"] = (
                "SmartTyping snippets (*.json)|*.json|All files (*.*)|*.*",
                "SmartTyping snippets (*.json)|*.json|ไฟล์ทั้งหมด (*.*)|*.*"),
        };
}
