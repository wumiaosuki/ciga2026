# AGENTS.md

本文件是给 Codex / AI 代理使用的项目协作提示词。对话、解释和计划默认使用中文。

## 项目概况

- 项目名：`ciga2026`
- Unity 版本：`6000.4.4f1`
- 渲染管线：URP `17.4.0`
- 输入系统：使用 Unity Input System，避免在运行时代码中直接使用旧版 `UnityEngine.Input`
- 主要运行时代码目录：`Assets/Scripts`
- 框架程序集：`Assets/Scripts/Ciga2026.Framework.asmdef`
- 框架命名空间根：`Ciga2026.Framework`

## 常用 MCP / 工具流程

优先使用 UnityMCP 操作 Unity，而不是猜测编辑器状态。

1. 修改 Unity 资源或脚本前，先读取编辑器状态：
   - `mcpforunity://editor/state`
   - 当前场景：`manage_scene(action="get_active")`
   - 控制台：`read_console`

2. 修改或新增脚本后：
   - 触发 Unity 刷新：`execute_menu_item(menu_path="Assets/Refresh")`
   - 等待 `editor_state.compilation.is_compiling == false`
   - 使用 `validate_script` 检查新增/修改脚本
   - 使用 `read_console(types=["error"])` 检查编译错误

3. 涉及 Unity API、包 API、Shader/VFX/URP 时：
   - 优先用 `unity_reflect` 验证类型和成员存在
   - 需要示例或细节时再用 `unity_docs`
   - 不要只依赖模型记忆写 Unity 6 API

4. 读取大场景或大量资源时：
   - `manage_scene(action="get_hierarchy")` 使用 `page_size`
   - `manage_asset(action="search")` 使用分页，默认不要生成 preview

## 已安装且推荐使用的包/插件

- UnityMCP：用于读写场景、资源、脚本、控制台。
- URP / Shader Graph / Visual Effect Graph：用于视觉、特效、shader。
- Input System：运行时输入优先使用 `UnityEngine.InputSystem`。
- Cinemachine：用于相机跟随、镜头控制、震屏。
- ProBuilder：用于快速搭建测试关卡和灰盒。
- DOTween / DOTween Pro：用于 UI、角色、镜头、特效的缓动动画，代码中使用 `DG.Tweening`。
- UI Particle (`com.coffee.ui-particle`)：用于在 uGUI 中渲染粒子，支持 UI 排序、Mask/RectMask2D、CanvasGroup alpha，不需要额外 Camera、RenderTexture 或 Canvas。
- Spine (`spine-unity 4.3.95` / `spine-csharp 4.3.36`)：用于 2D 骨骼动画。导出资源优先使用 Spine Editor 4.3.x，运行时代码使用 `Spine.Unity` / `Spine` 命名空间。
- TextMesh Pro：UI 文本优先使用 TMP。
- 2D Animation / Aseprite / PSD Importer / Tilemap / SpriteShape：适合 2D 和像素/骨骼/瓦片地图工作流。
- Timeline：适合过场和技能演出。

## 目录约定

- `Assets/Scripts/Framework`：项目轻量框架代码。
- `Assets/Scripts/Framework/Events`：事件总线。
- `Assets/Scripts/Framework/StateMachine`：状态机。
- `Assets/Scripts/Framework/Singletons`：Mono 单例基类。
- `Assets/Scripts/Framework/Audio`：音频管理。
- `Assets/Scripts/Framework/UI`：UI 管理。
- `Assets/Scripts/Framework/Utilities`：通用工具。

后续业务代码建议按模块拆到：

- `Assets/Scripts/Game`
- `Assets/Scripts/Game/Characters`
- `Assets/Scripts/Game/Combat`
- `Assets/Scripts/Game/UI`
- `Assets/Scripts/Game/Systems`

资源建议按类型拆到：

- `Assets/Art`
- `Assets/Audio`
- `Assets/Prefabs`
- `Assets/Materials`
- `Assets/ScriptableObjects`
- `Assets/VFX`

## 代码风格

- 运行时代码默认放入明确命名空间。
- 新增公共 API 需要 XML 注释，关键参数需要说明。
- 序列化字段建议使用中文 `Header` / `Tooltip`，方便在 Inspector 中配置。
- 优先保持框架轻量，避免为了“通用”引入复杂反射、服务定位器或大型依赖。
- 不要把 `Editor` 代码放入运行时程序集目录；需要 Editor 工具时放到 `Editor` 文件夹或独立 Editor asmdef。
- 避免在 `Update` 中做分配、查找对象或字符串拼接。
- 需要动画过渡时优先考虑 DOTween，但框架底层不强依赖 DOTween，除非明确需要。

## 框架使用约定

### 单例

- 可继承 `MonoSingleton<T>` 创建普通场景内单例。
- 可继承 `PersistentMonoSingleton<T>` 创建跨场景单例。
- 单例脚本应避免在 `Awake` 中假设其它单例已初始化。

### 事件总线

- 使用 `EventBus.Global.Subscribe<T>()` / `Publish<T>()` / `Unsubscribe<T>()`。
- 订阅通常在 `OnEnable`，取消订阅通常在 `OnDisable`。
- 事件类型优先使用明确的 struct/class，而不是过度依赖字符串事件。

### 状态机

- 状态实现 `IState`，或继承 `State` 只覆盖需要的方法。
- 状态切换走 `StateMachine.ChangeState`。
- 状态对象由宿主创建并注入依赖，不建议状态内部主动查找场景对象。

### 音频

- 使用 `AudioManager.Instance.PlayBgm(...)` 播放 BGM。
- 使用 `AudioManager.Instance.PlaySfx(...)` 播放短音效。
- 音量控制使用 `SetMasterVolume`、`SetBgmVolume`、`SetSfxVolume`。

### UI

- 使用 `UIManager.Instance.OpenPanel(...)` 从 prefab 打开 UI。
- UI prefab 可继承 `UIPanel` 获得 `OnShow`、`OnHide`、`OnRelease` 生命周期。
- 使用 `UILayer` 管理默认层级：`Background`、`Normal`、`Popup`、`Overlay`、`System`。
- 释放面板使用 `ReleasePanel`，不要手动 Destroy 已注册面板。
- UI 粒子优先使用 `Coffee.UIExtensions.UIParticle`，把 `UIParticle` 放在父节点，`ParticleSystem` 放在它的子节点。
- UI 粒子材质优先使用 `UI/Additive` 或 `UI/Default` 一类支持 uGUI/mask 的 shader，避免直接使用普通内置粒子 shader。

### Spine

- 世界/场景内角色优先使用 `Spine.Unity.SkeletonAnimation`。
- uGUI 内 Spine 角色或头像优先使用 `Spine.Unity.SkeletonGraphic`。
- 播放动画通常通过 `skeletonAnimation.AnimationState.SetAnimation(...)` / `AddAnimation(...)`。
- 需要在自定义 asmdef 里直接引用 Spine 类型时，在 asmdef references 中添加 `spine-unity`；不要让框架底层默认强依赖 Spine。
- Spine 导出版本保持 4.3.x，避免用更高主版本导出导致运行时数据不兼容。

## 资源和场景操作注意事项

- 修改场景前先确认 active scene。
- 对可复用对象优先创建 prefab。
- 新场景至少包含 Camera 和主光源，2D 场景可使用 2D Light。
- 不要删除或重置用户未要求处理的资源。
- 工作区可能已有未提交变更，不能随意 revert。

## 当前已知噪音

- DOTweenPro 在 Unity 6 下可能有 `CS0618` obsolete warning。
- UnityMCP 在 domain reload 后可能提示端口未释放并在 `6400/6401` 间切换；通常不影响使用。
- `vHierarchy` 是编辑器增强插件，Unity 6 obsolete warning 已在插件范围内隔离。
