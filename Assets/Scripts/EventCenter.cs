using System;
using System.Collections.Generic;
using Common;

// 事件类型枚举（可以根据业务扩展）
public enum EventCenterType
{
    Aim,
    UnAim,
    Fire,
    PlayerMove,
    UIUpdate,
    Hit,  // 子弹击中
}

/// <summary>
/// 事件中心（单例模式）：用于管理全局事件的注册、注销和触发
/// 采用 .NET 标准的 EventHandler<EventArgs> 委托，遵循事件驱动设计规范
/// </summary>
public class EventCenter : Singleton<EventCenter>
{
    // 存储事件类型与对应委托的字典（多播委托，支持多个订阅者）
    private readonly Dictionary<EventCenterType, EventHandler<EventArgs>> _eventDict = new Dictionary<EventCenterType, EventHandler<EventArgs>>();

    /// <summary>
    /// 注册事件监听
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="handler">事件处理委托</param>
    /// <exception cref="ArgumentNullException">委托为空时抛出异常</exception>
    public void Register(EventCenterType eventType, EventHandler<EventArgs> handler)
    {
        // 空委托校验，避免传入空值导致的问题
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler), "事件处理委托不能为null");
        }

        // 如果字典中没有该事件类型，先初始化一个空委托
        if (!_eventDict.ContainsKey(eventType))
        {
            _eventDict.Add(eventType, null);
        }

        // 多播委托添加：支持多个订阅者（不会覆盖已有委托）
        _eventDict[eventType] += handler;
    }

    /// <summary>
    /// 注销事件监听
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="handler">事件处理委托</param>
    /// <exception cref="ArgumentNullException">委托为空时抛出异常</exception>
    public void Unregister(EventCenterType eventType, EventHandler<EventArgs> handler)
    {
        // 空委托校验
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler), "事件处理委托不能为null");
        }

        // 如果字典中存在该事件类型，才执行移除操作
        if (_eventDict.ContainsKey(eventType))
        {
            // 多播委托移除：只移除对应的订阅者
            _eventDict[eventType] -= handler;

            // 可选：如果委托列表为空，移除该事件类型的键，节省内存
            if (_eventDict[eventType] == null)
            {
                _eventDict.Remove(eventType);
            }
        }
    }

    /// <summary>
    /// 触发事件（同步）
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="sender">事件发送者（通常是this）</param>
    /// <param name="args">事件参数（继承自EventArgs）</param>
    public void Emit(EventCenterType eventType, object sender, EventArgs args)
    {
        // 空参数校验：args可以为null（使用EventArgs.Empty）
        args ??= EventArgs.Empty;

        // 如果存在该事件类型的委托，执行触发
        if (_eventDict.TryGetValue(eventType, out EventHandler<EventArgs> handlers))
        {
            // 临时拷贝委托：避免触发过程中委托被修改（线程安全/防注销导致的空引用）
            EventHandler<EventArgs> tempHandlers = handlers;
            tempHandlers?.Invoke(sender, args);
        }
    }

    /// <summary>
    /// 清空所有事件（适用于场景切换、游戏重置时）
    /// </summary>
    public void ClearAllEvents()
    {
        _eventDict.Clear();
    }
}

