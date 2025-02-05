﻿// 画面読み込み時処理
window.onload = function () {
    document.getElementById('WF_ButtonLogOut').style.display = 'inline';

    //変更不可判断キー
    const DisabledKeyItem = document.getElementById('DisabledKeyItem').value;
    //非表示判断キー
    const VisibleKeyControlTable = document.getElementById('VisibleKeyControlTable').value;

    //入力不可キー
    document.getElementById('TxtSelLineCNT').readOnly = true; 
    document.getElementById('TxtCampCode').readOnly = true;

    //非表示項目
    switch (VisibleKeyControlTable) {
        case "LNM0007L": //固定費マスタ
            document.getElementById('KOTEIHI_LINE_TAISHOYM').style.display = "none";　//対象年月
            document.getElementById('KOTEIHI_LINE_GETSUGAKU').style.display = "none";　//月額運賃
            document.getElementById('KOTEIHI_LINE_GENGAKU').style.display = "none";　//減額対象額
            document.getElementById('KOTEIHI_LINE_KOTEIHIM').style.display = "none";　//月額固定費
            document.getElementById('KOTEIHI_LINE_KOTEIHID').style.display = "none";　//日額固定費
            document.getElementById('KOTEIHI_LINE_KAISU').style.display = "none";　//使用回数
            document.getElementById('KOTEIHI_LINE_KINGAKU').style.display = "none";　//金額
            document.getElementById('KOTEIHI_LINE_BIKOU').style.display = "none";　//備考
            break;
        case "LNM0007LSK": //SK固定費マスタ
            document.getElementById('KOTEIHI_LINE_STYMD').style.display = "none";　//有効開始日
            document.getElementById('KOTEIHI_LINE_ENDYMD').style.display = "none";　//有効終了日
            document.getElementById('KOTEIHI_LINE_RIKUBAN').style.display = "none";　//陸事番号
            document.getElementById('KOTEIHI_LINE_SYAGATA').style.display = "none";　//車型
            document.getElementById('KOTEIHI_LINE_KOTEIHIM').style.display = "none";　//月額固定費
            document.getElementById('KOTEIHI_LINE_KOTEIHID').style.display = "none";　//日額固定費
            document.getElementById('KOTEIHI_LINE_KAISU').style.display = "none";　//使用回数
            document.getElementById('KOTEIHI_LINE_KINGAKU').style.display = "none";　//金額
            document.getElementById('KOTEIHI_LINE_BIKOU1').style.display = "none";　//備考1
            document.getElementById('KOTEIHI_LINE_BIKOU2').style.display = "none";　//備考2
            document.getElementById('KOTEIHI_LINE_BIKOU3').style.display = "none";　//備考3
            break;
        case "LNM0007LTNG": //TNG固定費マスタ
            document.getElementById('KOTEIHI_LINE_STYMD').style.display = "none";　//有効開始日
            document.getElementById('KOTEIHI_LINE_ENDYMD').style.display = "none";　//有効終了日
            document.getElementById('KOTEIHI_LINE_RIKUBAN').style.display = "none";　//陸事番号
            document.getElementById('KOTEIHI_LINE_SYAGATA').style.display = "none";　//車型
            document.getElementById('KOTEIHI_LINE_SYABARA').style.display = "none";　//車腹
            document.getElementById('KOTEIHI_LINE_GETSUGAKU').style.display = "none";　//月額運賃
            document.getElementById('KOTEIHI_LINE_GENGAKU').style.display = "none";　//減額対象額
            document.getElementById('KOTEIHI_LINE_KOTEIHI').style.display = "none";　//固定費
            document.getElementById('KOTEIHI_LINE_BIKOU1').style.display = "none";　//備考1
            document.getElementById('KOTEIHI_LINE_BIKOU2').style.display = "none";　//備考2
            document.getElementById('KOTEIHI_LINE_BIKOU3').style.display = "none";　//備考3
            break;
    }

    //変更不可判断キーに値が入っていない場合
    if (DisabledKeyItem == "") {
        //名称を変更する
        document.getElementById('PAGE_NAME1').innerText = "固定費マスタ（追加）";
        document.getElementById('PAGE_NAME2').innerText = "固定費マスタ追加";
        document.getElementById('WF_ButtonUPDATE').value = "追加";
    }

    //変更不可判断キーに値が入っている場合、一意項目を入力不可にする
    if (DisabledKeyItem != "") { 
        //取引先コード
        document.getElementById('TxtTORICODE').readOnly = true;
        document.getElementById('TxtTORICODEcommonIcon').style.display = "none";
        //取引先名称
        document.getElementById('TxtTORINAME').readOnly = true;
        //加算先部門コード
        document.getElementById('TxtKASANORGCODE').readOnly = true;
        document.getElementById('TxtKASANORGCODEcommonIcon').style.display = "none";
        //加算先部門名称
        document.getElementById('TxtKASANORGNAME').readOnly = true;
        //有効開始日
        TxtStYMD.readOnly = true;
        document.getElementById('WF_StYMD_CALENDAR').style.display = "none";
    };

};

document.addEventListener("DOMContentLoaded", function () {
   var ControlTable = document.getElementById('VisibleKeyControlTable').value;
    switch (ControlTable) {
        case "LNM0007L": //固定費マスタ
            // カレンダー表示
            document.querySelectorAll('.datetimepicker').forEach(picker => {
                flatpickr(picker, {
                    wrap: true,
                    dateFormat: 'Y/m/d',
                    locale: 'ja',
                    clickOpens: false,
                    allowInput: true,
                    monthSelectorType: 'static',
                    //defaultDate: new Date() // 必要に応じてカスタマイズ
                });
            });
            break;
        case "LNM0007LSK": //SK固定費マスタ
            // カレンダー表示
            document.querySelectorAll('.datetimepicker').forEach(picker => {
                flatpickr(picker, {
                    wrap: true,
                    dateFormat: 'Y/m/d',
                    locale: 'ja',
                    clickOpens: false,
                    allowInput: true,
                    plugins: [
                        new monthSelectPlugin({
                            shorthand: true, //defaults to false
                            dateFormat: "Y/m",
                            altFormat: "F Y", //defaults to "F Y"
                            theme: "light" // defaults to "light"
                        })
                    ]
                });
            });
            break;
        case "LNM0007LTNG": //TNG固定費マスタ
            // カレンダー表示
            document.querySelectorAll('.datetimepicker').forEach(picker => {
                flatpickr(picker, {
                    wrap: true,
                    dateFormat: 'Y/m/d',
                    locale: 'ja',
                    clickOpens: false,
                    allowInput: true,
                    plugins: [
                        new monthSelectPlugin({
                            shorthand: true, //defaults to false
                            dateFormat: "Y/m",
                            altFormat: "F Y", //defaults to "F Y"
                            theme: "light" // defaults to "light"
                        })
                    ]
                });
            });
            break;
        }
});

