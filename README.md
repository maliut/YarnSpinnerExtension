## Save & Load

提供对话内存档和读档的能力。

在官方仓库中已经有两个 PR 试图解决这个问题（
[#65](https://github.com/YarnSpinnerTool/YarnSpinner/pull/65)
、
[#242](https://github.com/YarnSpinnerTool/YarnSpinner/pull/242)
），但并没有合并到主干中，也无法拿来即用。不过它们的思路还是可以借鉴的。

本方案则是利用反射，实现无侵入式的修改，避免了对源码的改动，更加便于使用。
同时对 demo 工程进行了适配，完善了很多细节问题。

所有逻辑都在 `SaveLoadManager.cs` 文件中，并附有注释可以参考。

注意点：
1. **（重要）为了存读档功能正常，不能使用 Yarn Spinner 自带的 Inline expressions 语法。**
    这是因为 Yarn Spinner 底层虚拟机没有提供指令级别的状态保存和恢复功能。当调用 `OnLineStart` 
   保存状态时，Inline expressions 已经被求值。但当调用 `ContinueDialogue` 恢复执行时，
   Inline expressions 又会被再次求值，导致虚拟机内部的栈错误。
   因此如需要这项功能，建议自己通过字符串替换等方式实现。
2. 代码只处理了对话中存档的情况。在真实项目中你还要考虑玩家在非对话状态中存档的情况，
   此时不应该调用 `ContinueDialogue` 启动对话系统。
3. 代码只保存了 Yarn Spinner 底层虚拟机的内部状态，对于 VariableStorage 和其他游戏状态你仍需自行保存。

---

This branch provides save and load support when dialogue running.

There are already 2 pull requests for this feature(
[#65](https://github.com/YarnSpinnerTool/YarnSpinner/pull/65)
、
[#242](https://github.com/YarnSpinnerTool/YarnSpinner/pull/242)
)，but both not merged, cannot be used out of the box. However 
their ideas can still be used for reference.

My solution is to achieve non-intrusive modifications by reflection.
It's easier to use, and fit for the demo project, perfected a lot of details.

All code logic can be found in `SaveLoadManager.cs`, with detailed comments.

Notice:
1. **(Important) For save and load function work properly, DO NOT use Yarn Spinner's
    Inline expressions syntax.** It's because there is lack support in the low-level 
   virtual machine for instruction-level save and load ability. 
   When `OnLineStart` is called to save the state, Inline expressions have been already 
   evaluated. But when `ContinueDialogue` is called to resume the state, 
   these Inline expressions will be evaluated again, which breaks the stack state in 
   virtual machine. So if you need this feature, I recommend achieving by yourself, 
   using string replacement or other ways.
2. The code only handles situation that users save when dialogue system running. 
    In real-world projects you should also handle situations when dialogue system not running. 
   In this case you should not call `ContinueDialogue` to wrongly launch the dialogue system.
3. The code only save the state of Yarn Spinner's low-level virtual machine. 
    For VariableStorage and other game states you should save them in your own ways.

