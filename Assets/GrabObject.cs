using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabObject : MonoBehaviour
{
    //필요 속성 : 물체를 잡고 있는지 여부, 잡고 있는 물체, 잡을 물체의 종류, 잡을 수 있는 거리
    // 물체를 잡고 있는지의 여부
    bool isGrabbing = false;
    //잡고 있는 물체
    GameObject grabbedObject;
    //잡을 물체의 종류
    public LayerMask grabbedLayer;
    //잡을 수 있는 거리
    public float grabRange = 0.2f;
    //이전 위치
    Vector3 prevPos;
    // 던질 힘
    float throwPower = 10;
    //이전 회전
    Quaternion prevRot;
    // 회전력
    public float rotPower = 5;
    // 원거리에서 물체를 잡는 기능 활성화 여부
    public bool isRemoteGrab = false;
    // 원거리에서 물체를 잡을 수 있는 거리
    public float remoteGrabDistance = 20;

    static bool[] istouch = new bool[2];

    public GameObject[] sign = new GameObject[2];
    public GameObject phone;
    public GameObject phoneImage;
    public GameObject warningImage;
    public GameObject okImage;



    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //물체 잡기
        //1. 물체를 잡지 않고 있을 경우
        if (isGrabbing == false)
        {
            //잡기시도
            TryGrab();
        }
        else
        {
            //물체 놓기
            TryUngrab();
        }
        if (ARAVRInput.GetDown(ARAVRInput.Button.HandTrigger, ARAVRInput.Controller.LTouch))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Physics.Raycast(ray, out hit);
            if(hit.collider.tag == "Phone")
            {
                Debug.Log("폰");
                phoneImage.SetActive(true);
                phone.SetActive(false);
                

            }
        }
    }

    private void TryUngrab()
    {
        //던질방향
        Vector3 throwDirection = (ARAVRInput.RHandPosition - prevPos);
        //위치기억
        prevPos = ARAVRInput.RHandPosition;

        Quaternion deltaRotation = ARAVRInput.RHand.rotation * Quaternion.Inverse(prevRot);
        prevRot = ARAVRInput.RHand.rotation;

        //버튼을 놓았다면
        if (ARAVRInput.GetUp(ARAVRInput.Button.HandTrigger, ARAVRInput.Controller.RTouch))
        {
            //잡지 않은 상태로 전환
            isGrabbing = false;
            //물리 기능 활성화
            grabbedObject.GetComponent<Rigidbody>().isKinematic = false;
            //손에서 폭탄 떼어내기
            grabbedObject.transform.parent = null;
            //던지기
            grabbedObject.GetComponent<Rigidbody>().velocity = throwDirection * throwPower;
            //각속도 = (1/dt) * d(특정 축 기준 변위 각도)
            float angle;
            Vector3 axis;
            deltaRotation.ToAngleAxis(out angle, out axis);
            Vector3 angularVelocity = (1.0f / Time.deltaTime) * angle * axis;
            grabbedObject.GetComponent<Rigidbody>().angularVelocity = angularVelocity;
            //잡은 물체가 없도록 설정
            grabbedObject = null;


        }
    }

    private void TryGrab()
    {
        //[Grab]버튼을 누르면 일정 영역 안에 있는 폭탄을 잡는다
        //1. [Grab] 버튼을 누렀다면 
        if (ARAVRInput.GetDown(ARAVRInput.Button.HandTrigger, ARAVRInput.Controller.RTouch))
        {
            // 원거리 물체 잡기를 사용한다면
            if (isRemoteGrab)
            {
                // 손 방향으로 Ray 제작
                Ray ray = new Ray(ARAVRInput.RHandPosition, ARAVRInput.RHandDirection);
                RaycastHit hitInfo;
                
                // SphereCast를 이용해 물체 충돌을 체크
                if (Physics.SphereCast(ray, 0.5f, out hitInfo, remoteGrabDistance, grabbedLayer))
                {
                    // 잡은 상태로 전환
                    isGrabbing = true;

                    // 잡은 물체에 대한 기억
                    grabbedObject = hitInfo.transform.gameObject;
                    // 물체가 끌려오는 기능 실행
                    StartCoroutine(GrabbingAnimation());
                }

                //2. 일정영역안에 폭탄이 있을때
                // 영역안에 있는 모든 폭탄 검출
                Collider[] hitObjects = Physics.OverlapSphere(ARAVRInput.RHandPosition, grabRange, grabbedLayer);
                //가장 가까운 폭탄 인덱스
                int closest = 0;
                //손과 가장 가까운 물체 선택
                for (int i = 1; i < hitObjects.Length; i++)
                {
                    //손과 가장 가까운 물체와의 거리
                    Vector3 closestPos = hitObjects[closest].transform.position;
                    float closestDistance = Vector3.Distance(closestPos, ARAVRInput.RHandPosition);
                    // 다음 물체와 손의 거리
                    Vector3 nextPos = hitObjects[i].transform.position;
                    float nextDistance = Vector3.Distance(nextPos, ARAVRInput.RHandPosition);
                    //다음 물체와의 거리가 더 가깝다면
                    if (nextDistance < closestDistance)
                    {
                        //가장 가까운 물체 인덱스 교체
                        closest = i;
                    }
                }
                //3. 폭탄을 잡는다
                //검출된 물체가 있을경우
                if (hitObjects.Length > 0)
                {
                    //잡은 상태로 전환
                    isGrabbing = true;
                    //잡은물체에 대한 기억
                    grabbedObject = hitObjects[closest].gameObject;
                    if (!istouch[0] || !istouch[1])
                    {
                        if (grabbedObject.tag.Equals("NoChair"))
                        {
                            warningImage.SetActive(true);
                            sign[0].SetActive(false);
                            Destroy(warningImage, 4);
                            istouch[0] = true;
                        }
                        else if (grabbedObject.tag.Equals("Chair"))
                        {
                            okImage.SetActive(true);
                            sign[1].SetActive(false);
                            Destroy(okImage, 4);
                            istouch[1] = true;
                        }
                    }
                    
                    //잡은 물체를 손의 자식으로 등록
                    grabbedObject.transform.parent = ARAVRInput.RHand;
                    // 물리 기능정지
                    grabbedObject.GetComponent<Rigidbody>().isKinematic = true;
                    //최기 위치 값 지정
                    prevPos = ARAVRInput.RHandPosition;
                    //초기 회전값 지정
                    prevRot = ARAVRInput.RHand.rotation;
                }
            }
        }

    }
    IEnumerator GrabbingAnimation()
    {
        // 물리 기능 정지
        grabbedObject.GetComponent<Rigidbody>().isKinematic = true;
        // 초기 위치 값 지정
        prevPos = ARAVRInput.RHandPosition;
        // 초기 회전 값 지정
        prevRot = ARAVRInput.RHand.rotation;
        Vector3 startLocation = grabbedObject.transform.position;
        Vector3 targetLocation = ARAVRInput.RHandPosition + ARAVRInput.RHandDirection * 0.1f;

        float currentTime = 0;
        float finishTime = 0.2f;

        // 경과율
        float elapsedRate = currentTime / finishTime;

        while (elapsedRate < 1)
        {
            currentTime += Time.deltaTime;
            elapsedRate = currentTime / finishTime;

            grabbedObject.transform.position = Vector3.Lerp(startLocation, targetLocation, elapsedRate);

            yield return null;
        }

        // 잡은 물체를 손의 자식으로 등록
        grabbedObject.transform.position = targetLocation;
        grabbedObject.transform.parent = ARAVRInput.RHand;



    }
}


