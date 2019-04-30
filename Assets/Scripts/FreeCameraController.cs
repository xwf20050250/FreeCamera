using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class FreeCameraController : MonoBehaviour
{
    // 模型
    public Transform model;
    // 默认距离
    private const float default_distance = 10f;
    private const float default_distance_max = 25f;
    private const float default_distance_min = 3.5f;

    private float distanceFromModel = 0f;

    public EventSystem eventSystem;
    public GraphicRaycaster graphicRaycaster;

    private Vector3 resetPosition;
    public static FreeCameraController Instance;

    // Use this for initialization
    void Start()
    {
        Instance = this;
        // 旋转归零
        transform.rotation = Quaternion.identity;
        // 初始位置是模型
        Vector3 position = model.position;
        position.z -= default_distance;
        transform.position = position;
        resetPosition = transform.position;
        UpdateDistanceFromModel();
    }

    void Update()
    {
        float dx, dy;
        dx = dy = 0;
        if (Input.touchSupported && Input.touchCount >= 1)
        {
            dx = Input.touches[0].deltaPosition.x * Time.deltaTime;
            dy = Input.touches[0].deltaPosition.y * Time.deltaTime;
        }
        else
        {
            dx = Input.GetAxis("Mouse X");
            dy = Input.GetAxis("Mouse Y");
        }

        // 旋转
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            //if (CheckGuiRaycastObjects()) return;
            //Debug.LogErrorFormat("X:{0}, Y:{1}", dx, dy);
            if (Mathf.Abs(dx) > 0 || Mathf.Abs(dy) > 0)
            {
                // 获取摄像机欧拉角
                Vector3 angles = transform.rotation.eulerAngles;
                // 欧拉角表示按照坐标顺序旋转，比如angles.x=30，表示按x轴旋转30°，dy改变引起x轴的变化
                angles.x = Mathf.Repeat(angles.x + 180f, 360f) - 180f;
                angles.y += dx;
                angles.x -= dy;
                // 设置摄像头旋转
                Quaternion rotation = Quaternion.identity;
                rotation.eulerAngles = new Vector3(angles.x, angles.y, 0);
                transform.rotation = rotation;
                // 重新设置摄像头位置
                Vector3 position = model.position;
                Vector3 distance = rotation * new Vector3(0, 0, distanceFromModel);
                transform.position = position - distance;
                //Debug.Log(string.Format("{0} {1} {2}", position, distance, transform.position));
            }
        }

        // window鼠标滚轮放大缩小
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetAxis("Mouse ScrollWheel") < 0 && 
            distanceFromModel <= default_distance_max)
        {
            transform.position -= transform.forward * 10f * Time.deltaTime;
            UpdateDistanceFromModel();
        }
        if (Input.GetAxis("Mouse ScrollWheel") > 0 && 
            distanceFromModel >= default_distance_min)
        {
            transform.position += transform.forward * 10f * Time.deltaTime;
            UpdateDistanceFromModel();
        }
#endif

        // 手势触摸放大缩小
        if (Input.touchSupported && Input.multiTouchEnabled)
        {
            if (GetEffectiveTouchCount() >= 2)
            {
                var handleTouch1 = GetEffectiveTouch(0);
                var handleTouch2 = GetEffectiveTouch(1);
                if (null != handleTouch1 && null != handleTouch2)
                {
                    var touch1 = handleTouch1.Value;
                    var touch2 = handleTouch2.Value;

                    if (TouchPhase.Began == touch1.phase || TouchPhase.Began == touch2.phase)
                    {
                        lastTouchesDistance = Vector2.Distance(touch1.position, touch2.position);
                    }
                    else
                    {
                        curTouchedDistance = Vector2.Distance(touch1.position, touch2.position);
                        deltaTouchDistance = curTouchedDistance - lastTouchesDistance;
                        if ((deltaTouchDistance > 0 && distanceFromModel >= default_distance_min) ||
                            (deltaTouchDistance < 0 && distanceFromModel <= default_distance_max))
                        {
                            if (deltaTouchDistance < 0 && (Mathf.Abs(deltaTouchDistance) + distanceFromModel > default_distance_max))
                            {
                                deltaTouchDistance = Mathf.Abs(deltaTouchDistance) + distanceFromModel - default_distance_max + deltaTouchDistance;
                            }
                            if (deltaTouchDistance > 0 && (distanceFromModel - deltaTouchDistance < default_distance_min))
                            {
                                deltaTouchDistance = default_distance_min - (distanceFromModel - distanceFromModel) + deltaTouchDistance;
                            }
                            transform.position += transform.rotation * new Vector3(0, 0, deltaTouchDistance) * Time.deltaTime * 0.2f;
                            lastTouchesDistance = curTouchedDistance;
                            UpdateDistanceFromModel();
                        }
                    }
                }
            }
        }
    }

    private float lastTouchesDistance = 0f;
    private float curTouchedDistance = 0f;
    private float deltaTouchDistance = 0f;

    private Touch? GetEffectiveTouch(int index)
    {
        int touchCount = Input.touchCount;
        if (touchCount < 1)
            return null;

        int idx = 0;
        for (int i = 0; i < touchCount; ++i)
        {
            var touch = Input.GetTouch(i);
            var ret = EventSystem.current.IsPointerOverGameObject(touch.fingerId);
            if (ret)
            {
                continue;
            }

            if (idx == index)
                return touch;

            idx++;
        }

        return null;
    }

    private int GetEffectiveTouchCount()
    {
        int touchCount = Input.touchCount;
        if (touchCount < 1)
            return touchCount;

        int effectiveTouchCount = 0;

        for (int i = 0; i < touchCount; ++i)
        {
            var touch = Input.GetTouch(i);
            var ret = EventSystem.current.IsPointerOverGameObject(touch.fingerId);
            if (!ret)
            {
                effectiveTouchCount += 1;
            }
        }
        return effectiveTouchCount;
    }

    bool CheckGuiRaycastObjects()
    {
        PointerEventData eventData = new PointerEventData(eventSystem);
        eventData.pressPosition = Input.mousePosition;
        eventData.position = Input.mousePosition;

        List<RaycastResult> list = new List<RaycastResult>();
        graphicRaycaster.Raycast(eventData, list);
        foreach (RaycastResult ret in list)
        {
            Debug.Log("ret: " + ret.gameObject.name);
        }
        return list.Count > 0;
    }

    public void ResetCamera()
    {
        transform.position = resetPosition;
        transform.rotation = Quaternion.identity;
        distanceFromModel = default_distance;
    }

    private void UpdateDistanceFromModel()
    {
        distanceFromModel = Vector3.Distance(transform.position, model.position);
    }
}