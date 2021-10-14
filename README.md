## Rich Text

为 Yarn Spinner 增加富文本展示的功能。

无论是默认的 UI.Text 还是 TextMeshPro 都支持类似 HTML 的富文本标签。但 demo 中使用了字符串分割的方式来实现打字效果，这会破坏富文本的展示。

因此我们必须换一个思路：在一句话开始时就把整行文本都设置上去，通过设置可见字符数来实现打字效果。这里我们选用了 TextMeshPro，因为它有现成的 `maxVisibleCharacters` 属性可以使用。

如果你选择默认的 UI.Text，也可以考虑重写 `OnPopulateMesh` 方法来实现同样的效果。

具体实现上，我们定义 `class TMP_DialogueUI : DialogueUI`，并重写 `RunLine` 方法。然后全局替换原来的 DialogueUI 组件即可。

---

Add rich text ability for Yarn Spinner.

Both UI.Text and TextMeshPro support rich-text tags like HTML. But the official demo uses string split for typing effect, which breaks rich text display.

So we should use other solution: when a line start, set the whole text to the component, and change visible character count to achieve typing effect. In this example, we choose TextMeshPro because we can just set its `maxVisibleCharacters` property.

If you want to use the default UI.Text, you can also consider override the `OnPopulateMesh` method to limit the visible character count.

In detail, we define `class TMP_DialogueUI : DialogueUI` and override `RunLine` method. Then replace the old DialogueUI component.