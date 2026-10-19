using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TuanjieAI.UI
{
    /// <summary>
    /// 游戏登录注册面板：管理登录与注册两个子面板的切换、输入校验及简单的内存用户存储。
    /// </summary>
    public class LoginRegisterPanel : MonoBehaviour
    {
        [Header("面板引用")]
        public GameObject loginPanel;
        public GameObject registerPanel;

        [Header("登录面板控件")]
        public InputField loginUsernameInput;
        public InputField loginPasswordInput;
        public Button loginButton;
        public Button switchToRegisterButton;
        public Text loginMessageText;

        [Header("注册面板控件")]
        public InputField registerUsernameInput;
        public InputField registerPasswordInput;
        public InputField registerConfirmPasswordInput;
        public Button registerButton;
        public Button switchToLoginButton;
        public Text registerMessageText;

        /// <summary>登录成功时触发，参数为用户名。</summary>
        public event System.Action<string> OnLoginSuccess;

        /// <summary>注册成功时触发，参数为用户名。</summary>
        public event System.Action<string> OnRegisterSuccess;

        // 简单的内存用户数据库：用户名 → 密码
        private static readonly Dictionary<string, string> UserDatabase = new Dictionary<string, string>();

        private void Start()
        {
            BindUI();
            ShowLoginPanel();
        }

        private void BindUI()
        {
            loginButton.onClick.AddListener(OnLoginClicked);
            switchToRegisterButton.onClick.AddListener(ShowRegisterPanel);
            registerButton.onClick.AddListener(OnRegisterClicked);
            switchToLoginButton.onClick.AddListener(ShowLoginPanel);

            // 密码输入框以 * 遮蔽显示
            loginPasswordInput.contentType = InputField.ContentType.Password;
            registerPasswordInput.contentType = InputField.ContentType.Password;
            registerConfirmPasswordInput.contentType = InputField.ContentType.Password;

            // 回车提交
            loginPasswordInput.onEndEdit.AddListener(value =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    OnLoginClicked();
            });
            registerConfirmPasswordInput.onEndEdit.AddListener(value =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    OnRegisterClicked();
            });
        }

        // ────────────── 面板切换 ──────────────

        public void ShowLoginPanel()
        {
            loginPanel.SetActive(true);
            registerPanel.SetActive(false);
            loginMessageText.text = string.Empty;
        }

        public void ShowRegisterPanel()
        {
            loginPanel.SetActive(false);
            registerPanel.SetActive(true);
            registerMessageText.text = string.Empty;
        }

        // ────────────── 登录逻辑 ──────────────

        private void OnLoginClicked()
        {
            string username = loginUsernameInput.text.Trim();
            string password = loginPasswordInput.text;

            if (string.IsNullOrEmpty(username))
            {
                SetLoginMessage("请输入用户名", Color.red);
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                SetLoginMessage("请输入密码", Color.red);
                return;
            }

            if (UserDatabase.TryGetValue(username, out string storedPassword))
            {
                if (storedPassword == password)
                {
                    SetLoginMessage($"登录成功！欢迎，{username}", Color.green);
                    Debug.Log($"[LoginRegister] 用户 '{username}' 登录成功");
                    OnLoginSuccess?.Invoke(username);
                }
                else
                {
                    SetLoginMessage("密码错误", Color.red);
                }
            }
            else
            {
                SetLoginMessage("该用户不存在，请先注册", Color.red);
            }
        }

        // ────────────── 注册逻辑 ──────────────

        private void OnRegisterClicked()
        {
            string username = registerUsernameInput.text.Trim();
            string password = registerPasswordInput.text;
            string confirmPassword = registerConfirmPasswordInput.text;

            if (string.IsNullOrEmpty(username))
            {
                SetRegisterMessage("请输入用户名", Color.red);
                return;
            }
            if (username.Length < 3)
            {
                SetRegisterMessage("用户名至少需要3个字符", Color.red);
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                SetRegisterMessage("请输入密码", Color.red);
                return;
            }
            if (password.Length < 6)
            {
                SetRegisterMessage("密码至少需要6个字符", Color.red);
                return;
            }
            if (password != confirmPassword)
            {
                SetRegisterMessage("两次输入的密码不一致", Color.red);
                return;
            }
            if (UserDatabase.ContainsKey(username))
            {
                SetRegisterMessage("该用户名已被注册", Color.red);
                return;
            }

            UserDatabase[username] = password;
            SetRegisterMessage($"注册成功！欢迎，{username}", Color.green);
            Debug.Log($"[LoginRegister] 新用户 '{username}' 注册成功");

            // 自动填充登录框并切换到登录面板
            loginUsernameInput.text = username;
            loginPasswordInput.text = string.Empty;
            OnRegisterSuccess?.Invoke(username);

            // 延迟切换到登录面板（让用户看到成功消息）
            Invoke(nameof(ShowLoginPanel), 1.2f);
        }

        // ────────────── 辅助方法 ──────────────

        private void SetLoginMessage(string msg, Color color)
        {
            loginMessageText.text = msg;
            loginMessageText.color = color;
        }

        private void SetRegisterMessage(string msg, Color color)
        {
            registerMessageText.text = msg;
            registerMessageText.color = color;
        }
    }
}
