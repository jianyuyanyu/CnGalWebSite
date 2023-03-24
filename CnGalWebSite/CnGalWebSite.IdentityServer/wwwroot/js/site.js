﻿var HttpClient = function () {
    this.get = function (aUrl, aCallback) {
        var anHttpRequest = new XMLHttpRequest();
        anHttpRequest.onreadystatechange = function () {
            if (anHttpRequest.readyState == 4 && anHttpRequest.status == 200)
                aCallback(anHttpRequest.responseText);
        }

        anHttpRequest.open("GET", aUrl, true);
        anHttpRequest.send(null);
    }
}

function callGeetest(buttonId = 'geetestCode-submit-button') {
    if (document.getElementById('geetestResult-validate').value != null&&document.getElementById('geetestResult-validate').value != '') {
        return true;
    }
    var client = new HttpClient();
    client.get('https://oauth2.cngal.org/api/tool/GetGeetestCode', function (response) {
        //获取页面初始化数据
        re= JSON.parse(response);
        gt = re.gt;
        challenge = re.challenge;
        success = re.success;

        initGeetest({
            // 以下配置参数来自服务端 SDK
            gt: gt,
            challenge: challenge,
            offline: !success,
            new_captcha: true,
            width: '100%',
            product: 'bind'
        }, function (captchaObj) {
            // 这里可以调用验证实例 captchaObj 的实例方法
            captchaObj.appendTo('#captcha-box');
            captchaObj.onReady(function () {
                //验证码ready之后才能调用verify方法显示验证码
                captchaObj.verify();
            });
            //成功回调
            captchaObj.onSuccess(function () {
                var result = captchaObj.getValidate();
                //设置数据
                document.getElementById('geetestResult-validate').value = result.geetest_validate;
                document.getElementById('geetestResult-challenge').value = result.geetest_challenge;
                document.getElementById('geetestResult-seccode').value = result.geetest_seccode;
                //提交表单
                document.getElementById(buttonId).click();
            });
        });
    });
    return false;
}
