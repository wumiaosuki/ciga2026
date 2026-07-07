# 玩法运行配置 JSON 说明

配置文件路径：

```text
Assets/StreamingAssets/GameplaySessionConfig.json
```

打包后也可以修改同名 JSON 文件来调整关键数值。进入游戏时会重新读取一次配置；如果 JSON 不存在或格式错误，会自动使用 Unity 内 `DefaultGameplaySessionConfig` 的默认值。

成功应用 JSON 时，Unity Console 会出现类似日志：

```text
已应用玩法 JSON 配置：.../GameplaySessionConfig.json，初始容忍度=50，容忍度上限=100，基础选词时间=5，超时扣分=10
```

## 字段说明

### initialTolerance

初始容忍度。

```json
"initialTolerance": 50
```

含义：一局开始时玩家拥有的容忍度。容忍度降到 0 时游戏失败。

建议：必须大于 0。

### maxTolerance

容忍度上限。

```json
"maxTolerance": 100
```

含义：玩家通过 A/B 评分回复容忍度后，最高不会超过这个值。界面进度条和数字分母也使用这个上限。

建议：应大于等于 `initialTolerance`。如果配置得比初始值小，程序会按初始值作为实际上限。

### gradeThresholds

累计扣分对应评分档位。

```json
"gradeThresholds": [
  { "grade": 0, "maxTotalPenalty": 0 },
  { "grade": 1, "maxTotalPenalty": 10 },
  { "grade": 2, "maxTotalPenalty": 30 },
  { "grade": 3, "maxTotalPenalty": 50 },
  { "grade": 4, "maxTotalPenalty": 70 }
]
```

含义：提交时根据本关累计扣分，从上到下匹配第一个 `maxTotalPenalty` 大于等于当前扣分的档位。

`grade` 对应关系：

```text
0 = A
1 = B
2 = C
3 = D
4 = E
```

例子：累计扣分为 20 时，会跳过 A 和 B，命中 C。

注意：建议按 A 到 E 从小到大填写，否则会先命中靠前的配置。

### gradeARecovery

A 评分回复的容忍度。

```json
"gradeARecovery": 20
```

含义：每次提交获得 A 时，回复多少容忍度。回复后不会超过 `maxTolerance`。

### gradeBRecovery

B 评分回复的容忍度。

```json
"gradeBRecovery": 10
```

含义：每次提交获得 B 时，回复多少容忍度。回复后不会超过 `maxTolerance`。

### consecutiveAGradeThreshold

连续 A 触发额外回复的次数。

```json
"consecutiveAGradeThreshold": 2
```

含义：连续获得多少次 A 后，触发额外回复。

例子：配置为 2 时，第 2 次连续 A 开始会额外回复。

### consecutiveAGradeRecoveryBonus

连续 A 的额外回复值。

```json
"consecutiveAGradeRecoveryBonus": 10
```

含义：达到连续 A 条件后，A 评分会在 `gradeARecovery` 基础上额外回复该数值。

例子：

```text
gradeARecovery = 20
consecutiveAGradeThreshold = 2
consecutiveAGradeRecoveryBonus = 10
```

第 1 次 A 回复 20。第 2 次连续 A 回复 30。之后只要连续 A 不断，也回复 30。

### initialLevelDuration

基础选词倒计时时间，单位秒。

```json
"initialLevelDuration": 100.0
```

含义：每次选词后的基础倒计时。最终显示时间还会乘以 `levelDurationMultiplierCurve`。

例子：基础时间为 100，当前关卡曲线倍率为 0.5，则本次选词倒计时为 50 秒。

### selectionTimeoutPenalty

选词倒计时清零时的扣分。

```json
"selectionTimeoutPenalty": 10
```

含义：玩家在一次选词倒计时内没有操作，倒计时归零时扣除的容忍度。这个扣分不计入本关评分扣分，不影响最终 A/B/C/D/E 判定。

### levelDurationMultiplierCurve

关卡进度对应的倒计时倍率曲线。

```json
"levelDurationMultiplierCurve": [
  { "time": 0.0, "value": 1.0 },
  { "time": 0.5, "value": 0.72 },
  { "time": 1.0, "value": 0.45 }
]
```

含义：随着关卡推进，倒计时时间会乘以不同倍率。

`time` 含义：

```text
0.0 = 第一关
0.5 = 中间进度
1.0 = 最后一关
```

`value` 含义：倒计时倍率。

例子：

```text
initialLevelDuration = 100
value = 0.72
实际倒计时 = 72 秒
```

注意：`time` 建议保持 0 到 1，`value` 建议大于 0。

### warningPenaltyThreshold

高扣分警告音效阈值。

```json
"warningPenaltyThreshold": 20
```

含义：单次选词扣分达到该数值时，播放警告音效。

例子：配置为 20 时，选择扣分 20 或更高的错误词会播放警告音效。

## 完整示例

```json
{
  "initialTolerance": 50,
  "maxTolerance": 100,
  "gradeThresholds": [
    { "grade": 0, "maxTotalPenalty": 0 },
    { "grade": 1, "maxTotalPenalty": 10 },
    { "grade": 2, "maxTotalPenalty": 30 },
    { "grade": 3, "maxTotalPenalty": 50 },
    { "grade": 4, "maxTotalPenalty": 70 }
  ],
  "gradeARecovery": 20,
  "gradeBRecovery": 10,
  "consecutiveAGradeThreshold": 2,
  "consecutiveAGradeRecoveryBonus": 10,
  "initialLevelDuration": 100.0,
  "selectionTimeoutPenalty": 10,
  "levelDurationMultiplierCurve": [
    { "time": 0.0, "value": 1.0 },
    { "time": 0.5, "value": 0.72 },
    { "time": 1.0, "value": 0.45 }
  ],
  "warningPenaltyThreshold": 20
}
```

## 修改注意事项

- JSON 字段名区分大小写，不要改字段名。
- 不要在 JSON 中写注释。
- 数字后不要漏逗号，也不要多写尾逗号。
- 音频、图片、Prefab 等资源引用暂时不走这个 JSON，仍在 Unity 内配置。
- 修改 JSON 后，重新进入游戏流程即可读取新值。
