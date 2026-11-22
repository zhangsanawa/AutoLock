# 如何把 AutoLock 传到 GitHub

> 假设你已经注册了 GitHub 账号。全程不需要命令行。

---

## 第一步：创建仓库

1. 打开 https://github.com/new
2. 填写：
   - **Repository name**：`autolock`
   - **Description**：`空闲自动锁屏，不关屏幕不断蓝牙 | Auto lock screen after idle without turning off display or Bluetooth`
   - 选择 **Public**（公开，别人才能搜到）
   - **不要**勾选 "Add a README file"（我们自己有）
   - **不要**选 .gitignore 和 license（我们自己有）
3. 点 **Create repository**

---

## 第二步：上传文件（最简单的方法：GitHub Desktop）

### 方法 A：GitHub Desktop（推荐，能保留文件夹结构）

1. 下载 https://desktop.github.com/ 并安装
2. 用你的 GitHub 账号登录
3. 菜单 **File → Add local repository**
4. 选择文件夹 `F:\DSH文件\AutoLock`
5. 如果提示 "not a Git repository"，点 **create a repository** 然后确认
6. 左下角填写提交信息 `Initial release v1.0.0`，点 **Commit to main**
7. 点顶部 **Publish repository**
8. **取消勾选** "Keep this code private"，点 **Publish**

完成。所有文件和文件夹结构都完整上传了。

### 方法 B：网页上传（适合文件少的情况）

1. 打开 `https://github.com/zhangsanawa/autolock/upload/main`
2. 依次拖入这些文件：

```
AutoLock.cs
AutoLock.exe
AutoLock.exe.config
app.manifest
app.ico
icon.svg
build.bat
README.md
LICENSE
.gitignore
```

3. 提交信息填 `Initial release v1.0.0`，点 **Commit changes**

4. 再上传 `installer` 文件夹里的两个文件：
   - 打开 `https://github.com/zhangsanawa/autolock/upload/main/installer`
   - 拖入 `AutoLock.iss` 和 `default.ini`

5. 再上传安装包：
   - 打开 `https://github.com/zhangsanawa/autolock/upload/main/dist`
   - 拖入 `AutoLock-Setup-1.0.0.exe`

> **注意**：`config.ini` 和 `autolock.log` **不要传**。
> 那是本机运行时生成的文件，已经写进 `.gitignore` 了。

---

## 第三步：把安装包做成 Release（强烈推荐）

上传的文件别人要进文件夹才能下载，做成 Release 更专业。

1. 进入仓库页面
2. 右侧找到 **Releases** → 点 **Create a new release**
3. **Choose a tag**：输入 `v1.0.0`，点 "Create new tag: v1.0.0 on publish"
4. **Release title**：`AutoLock v1.0.0`
5. **Describe this release**：粘贴下面内容

```markdown
## AutoLock v1.0.0

空闲 N 分钟后自动锁屏，**不关屏幕、不断蓝牙**。

### 适用场景
电脑不会自动锁屏，但一睡眠就断蓝牙音箱/耳机（尤其是 USB 蓝牙适配器）的用户。

### 下载
- `AutoLock-Setup-1.0.0.exe` — 安装版（推荐）
- `AutoLock.exe` — 绿色版，双击即用

### 系统要求
Windows 10 1607+ / Windows 11，需要 .NET Framework 4.8（Win10 1903+ 系统自带）

### 说明
安装包未做代码签名，SmartScreen 可能提示，点「更多信息 → 仍要运行」即可。
```

6. 找到下方 **Attach binaries by dropping them here**
7. 把 `dist/AutoLock-Setup-1.0.0.exe` 拖进去
8. 点 **Publish release**

---

## 第四步：让别人能搜到（重要）

GitHub 搜索主要看 **仓库名**、**Description** 和 **Topics**。

### 设置 Topics

仓库页面右上角 **⚙️**（About 区域旁边）→ **Topics** 里填：

```
windows
autolock
lock-screen
idle
bluetooth
windows-10
windows-11
screensaver
productivity
csharp
```

### 确认 Description

About 里的描述要包含关键词：

```
空闲10分钟自动锁屏，不关屏幕不断蓝牙 | Auto lock Windows after idle without turning off display or disconnecting Bluetooth speakers
```

---

## 第五步（可选）：加预览图

1. 截图你的托盘菜单
2. 在仓库页面点 **Issues → New issue**，把图片拖进输入框
3. GitHub 会生成一个 `https://user-images.githubusercontent.com/...` 链接
4. 复制那个链接，在 `README.md` 第一行后面加：

```markdown
![AutoLock 托盘菜单](粘贴你的图片链接)
```

5. 提交修改，然后关掉那个 issue

---

## 常见问题

**Q：网页上传时文件夹拖不进去？**
A：GitHub 网页不支持直接拖文件夹。用方法 A（GitHub Desktop）最省事。

**Q：文件大小有限制吗？**
A：单文件 100 MB。你的安装包 2.4 MB，没问题。

**Q：可以改仓库名吗？**
A：可以。Settings → Repository name 直接改，GitHub 会自动重定向旧链接。

**Q：怎么发布新版本？**
A：改代码 → 重新编译 → 网页上更新文件 → 发新 Release（tag 用 `v1.0.1`）。

**Q：别人下载时 Windows 报病毒怎么办？**
A：未签名的 exe 常见提示。README 里已说明"点更多信息 → 仍要运行"。
   想彻底解决需要买代码签名证书（约 300-500 美元/年），个人项目一般不做。