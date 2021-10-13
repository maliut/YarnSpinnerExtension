using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Yarn.Unity.Example
{
    public class SaveLoadManager : MonoBehaviour
    {

        private const BindingFlags PublicFlag = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags PrivateFlag = BindingFlags.NonPublic | BindingFlags.Instance;

        private DialogueRunner runner;
        private Dialogue dialogue => runner.Dialogue;

        private void Start()
        {
            runner = FindObjectOfType<DialogueRunner>();
        }

        private void OnGUI()
        {
            // 点击此按钮保存当前的对话状态
            if (GUI.Button(new Rect(0, 0, 200, 100), "save")) {
                // 这里使用自带的 JsonUtility 进行序列化，实际可以选用其他方式进行序列化
                var json = JsonUtility.ToJson(currentState);
                // Debug.Log("save " + json);
                // 这里把序列化后的内容保存到 PlayerPrefs 中，实际可以选用其他持久化的方式
                PlayerPrefs.SetString("saved", json);
            }
            // 点击此按钮读取之前保存的对话状态
            if (GUI.Button(new Rect(0, 100, 200, 100), "load")) {
                var json = PlayerPrefs.GetString("saved");
                // Debug.Log("load " + json);
                var stateWrapper = JsonUtility.FromJson<VmStateWrapper>(json);
                // 获取当前 vm 的 state
                var vmFieldInfo = typeof(Dialogue).GetField("vm", PrivateFlag);
                var vm = vmFieldInfo!.GetValue(dialogue);
                var vmStateFieldInfo = vm.GetType().GetField("state", PrivateFlag);
                var vmState = vmStateFieldInfo!.GetValue(vm); // VirtualMachine.State
                // 用读取出来的 state 覆盖掉当前的 state
                stateWrapper.Overwrite(vmState);
                // 还需要设置 vm.currentNode，因为 state 只保存了 currentNodeName，因此还要根据 nodeName 把 currentNode 恢复出来
                // 注意这里需要确保 program 加载完成。一般都是在启动时加载，所以问题不大。
                var program = (Program) typeof(Dialogue)
                    .GetField("_program", PrivateFlag)!.GetValue(dialogue);
                var currentNode = program.Nodes[stateWrapper.currentNodeName];
                vm.GetType().GetField("currentNode", PrivateFlag)!.SetValue(vm, currentNode);
                
                var dialogueUI = (DialogueUI) runner.dialogueUI;
                // 重置 dialogUI，否则在选项状态时读档会导致选项没有被隐藏
                dialogueUI.dialogueContainer.SetActive(false);
                foreach (var button in dialogueUI.optionButtons) {
                    button.gameObject.SetActive(false);
                }
                // 重置 vm.executionState 为 suspended，否则读档时若处在选项状态会报错
                // 改成 stopped 应该也行，但是不能直接调用 runner.Stop(), 因为内部会重置 vm.state
                vm.GetType().GetField("_executionState", PrivateFlag)!.SetValue(vm, 2);
                // 类似 DialogRunner#StartDialogue, 但是不能调用 Dialogue.SetNode，因为内部会重置 vm.state
                dialogueUI.StopAllCoroutines();
                runner.IsDialogueRunning = true;
                dialogueUI.DialogueStarted();
                typeof(DialogueRunner).GetMethod("ContinueDialogue", PrivateFlag)!
                    .Invoke(runner, new object[0]);
            }
        }

        private VmStateWrapper currentState;

        /// <summary>
        /// 在每一行开始的时候获取当前 vm 的 state。注意要把这个方法注册到 dialogRunner 的相应回调中
        /// 之所以要在这里获取 state，而不是点击保存按钮时再获取，是因为虚拟机的实现在执行完指令后会把 programCounter++
        /// 这样会导致读取到的状态实际是下一句话
        /// </summary>
        public void OnLineStart()
        {
            currentState = new VmStateWrapper(GetVmState());
        }
        
        /// <summary>
        /// 通过反射获取虚拟机当前的状态 vm.state
        /// </summary>
        /// <returns>vm.state</returns>
        private object GetVmState()
        {
            var vmFieldInfo = typeof(Dialogue).GetField("vm", PrivateFlag);
            var vm = vmFieldInfo!.GetValue(dialogue);
            var vmStateFieldInfo = vm.GetType().GetField("state", PrivateFlag);
            var vmState = vmStateFieldInfo!.GetValue(vm); // VirtualMachine.State
            return vmState;
        }

        /// <summary>
        /// 包裹 vm.state, 使之变为 JsonUtility 可以序列化的形式
        /// </summary>
        [Serializable]
        private class VmStateWrapper
        {

            public string currentNodeName;
            public int programCounter;
            public SerializableOption[] currentOptions;
            public SerializableYarnValue[] stackValues;
            
            public VmStateWrapper(object rawState)
            {
                var vmStateKlass = rawState.GetType(); // VirtualMachine.State
                currentNodeName = (string) vmStateKlass
                    .GetField("currentNodeName", PublicFlag)!.GetValue(rawState);
                programCounter = (int) vmStateKlass
                    .GetField("programCounter", PublicFlag)!.GetValue(rawState);
                var rawOptions = (List<KeyValuePair<Line, string>>) vmStateKlass
                    .GetField("currentOptions", PublicFlag)!.GetValue(rawState);
                currentOptions = new SerializableOption[rawOptions.Count];
                for (var i = 0; i < rawOptions.Count; i++) {
                    currentOptions[i] = new SerializableOption {
                        ID = rawOptions[i].Key.ID,
                        Substitutions = rawOptions[i].Key.Substitutions,
                        Value = rawOptions[i].Value
                    };
                }
                var rawStack = (Stack<Value>) vmStateKlass
                    .GetField("stack", PrivateFlag)!.GetValue(rawState);
                var rawArray = rawStack.ToArray();
                stackValues = new SerializableYarnValue[rawArray.Length];
                for (var i = 0; i < rawArray.Length; i++) {
                    stackValues[i] = new SerializableYarnValue {
                        ValueType = rawArray[i].type,
                        StringValue = rawArray[i].AsString
                    };
                }
            }
            
            /// <summary>
            /// 把存档里反序列化出来的内容覆盖掉当前的 vm.state
            /// </summary>
            public void Overwrite(object rawState)
            {
                var vmStateKlass = rawState.GetType();
                vmStateKlass.GetField("currentNodeName", PublicFlag)!
                    .SetValue(rawState, currentNodeName);
                vmStateKlass.GetField("programCounter", PublicFlag)!
                    .SetValue(rawState, programCounter);
                var rawOptions = new List<KeyValuePair<Line, string>>(currentOptions.Length);
                rawOptions.AddRange(currentOptions.Select(option => new KeyValuePair<Line, string>(new Line {
                    ID = option.ID,
                    Substitutions = option.Substitutions
                }, option.Value)));
                vmStateKlass.GetField("currentOptions", PublicFlag)!
                    .SetValue(rawState, rawOptions);
                var rawStack = new Stack<Value>(stackValues.Length);
                foreach (var value in stackValues) {
                    switch (value.ValueType) {
                        case Value.Type.Null:
                            rawStack.Push(Value.NULL);
                            break;
                        case Value.Type.Bool:
                            rawStack.Push(new Value(bool.Parse(value.StringValue)));
                            break;
                        case Value.Type.Number:
                            rawStack.Push(new Value(float.Parse(value.StringValue)));
                            break;
                        case Value.Type.String:
                            rawStack.Push(new Value(value.StringValue));
                            break;
                        // 其他类型在序列化时调用 AsString 已经报错了，因此理论上不会出现
                    }
                }
                vmStateKlass.GetField("stack", PrivateFlag)!
                    .SetValue(rawState, rawStack);
            }

            [Serializable]
            public struct SerializableOption
            {
                public string ID;
                public string[] Substitutions;
                public string Value;
            }
            
            [Serializable]
            public struct SerializableYarnValue
            {
                public Value.Type ValueType;
                public string StringValue;
            }
        }
    }
}
