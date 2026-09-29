using System;
using System.Collections.Generic;
using UnityEngine;

#region 유틸리티 : CPrint
/*
 
 - 유틸리티 : CPrint
  - 구조화를 시키고 싶다 => 출력에 관한것을

  - 프린트는 콘솔 프로젝트에서 사용한 것 처럼 출력 규칙을 유니티 스럽게 바꾼 버전

  - 유니티에서 뭔가를 만들려고 할때...

   C# => Main

   유니티 => 유니티 생성주기에 올리고 대신 호출을 하게 된다 (이벤트 기반)

   유니티에서는..... global using을 권장하지 않는다
    ㄴ 자동화가 완벽하게 안된다

   유니티에서 전역 스럽게 사용하고 싶다면
    ㄴ 글로벌 네임스페이스 => static
       ㄴ 별다른 using 없어도 잘 들어감

    ㄴ 네임 스페이스는 유지하되 풀네임 호출로 통일한다
       ㄴ  EX : Common.CPrint.Title("");
       ㄴ 어디 소속인지 명확하다

    ㄴ using static
       ㄴ 추적이 어려움

  => CPrint 업그레이드
   
  - 로그 => 구조 / 종류
  - CPrint는 런타임에서도 쓸 수 있기 때문에 기본은 Eidtor 처리를 하지 않는다
    ㄴ 필요하다면 전용 유틸로 분리한다
   
 
 */
#endregion

public static class CPrint
{
    // 옵션
    // 스위치
    public static bool Enable = true;
    // 서식 태그 (콘솔 컬러 태그)
    public static bool EnableRichText = true;

    // 들여쓰기
    // => 가독성을 위해 => 출력 앞에 공백을 붙여 구조적으로 분리하기가 좋다
    //  ㄴ 묶음 / 같은 덩어리인지..? => 트리 구조처럼 만들겠다 => 가독성
    private static int _indentLevel = 0;
    private const int INDENT_SPACES = 2;

    // HashSet : 중복을 허용하지 않고 고유한 요소만 저장한다 (자료구조)
    // ㄴ 일반적으로 O(1) 시간복잡도 수준을 보인다
    private static readonly HashSet<string> _onceSet = new HashSet<string>();

    /*
      readonly
       ㄴ 컴파일x / 런o
      - 한번 정해진 이후에는 다시 대입하지 못하게 막는다
       ㄴ 초기화 => 선언부에서 하거나 / 생성자에서 하거나

      - MonoBehaviour 때문에 생성자를 직접적으로 사용하는 경우는 C# 대비 맞지 않다

      => 해시셋
      - 컬렉션 클래스 => 해시 테이블 기반 => 데이터 구조
       ㄴ 중복되지 않은 요소들의 모임을 관리 / 이럴 경우 최적화 되어 있고 탐색이 빠름 => 추가 및 삭제도 가능

      해시 테이블
      - 키 / 값이 쌍으로 데이터를 저장하는 자료구조
      - 키를 이용해 (해시 함수) 특정 인덱스로 접근 (혹은 반환) => 데이터 저장 => 삽입 / 삭제 / 검색이 빠르다

      - 내부 동작
       1. 해시 함수
        - 값을 => 해시 코드로(정수) 바꾼다
         ㄴ 같은 값이면 같은 해시 코드가 나오는게 이 자료구조의 목표이기 때문
         ㄴ 1(해시 코드) 1(해시 코드)

      
       2. 버킷
        - 해시 코드를 기준으로 저장 위치(버킷)을 고른다
        ㄴ 대충 "해시코드 % 버킷개수" 같은 방식으로 인덱스를 결정한다고 생각하면 된다

       3. 충돌
        - 서로 다른 값인데 해시 코드가 겹칠 수 있다 (+버킷)
        ㄴ 버킷 안에서 추가 비교등을 수행해서 진짜 같은 값인지 확인
        - HaahSet => 해시 + 실제 비교를 같이 쓴다

       4. 재해싱
        - 안에 요소가 많아지면 => 버킷이 뻑뻑해 진다 => 성능 떨어질 수 있음

        * 중복 없이 저장 + 빠른 컨테이너
        * 내부는 해시로 위치 찾고 => 충돌은 비교로 해결 => 많아지면 재해싱
    */

    // 들여쓰기 문자열
    private static string Indent
    {
        get
        {
            // 레벨 * 공백수
            // 로그쪽으로..
            return new string(' ', _indentLevel * INDENT_SPACES);
        }
    }

    // 단계별 출력을 줄 맞춰서 읽기 쉽게 만든다    
    public static void IndentPush()
    {
        // 단계 올리기
        _indentLevel++;
    }

    private enum EnumELogKind
    {
        Log,
        Warn,
        Error,
        Success,
    }

    // 출력 포멧 관리를 위해
    // 들여쓰기 / 접두사 / 리치 텍스트 => Kind 분류
    private static void Emit(EnumELogKind kind, string msg, string tag = null, string colorHex = null, UnityEngine.Object context = null)
    {
        // 지금까지 만든 문자열을 콘솔로 내보내는 출력 코어
        // 색상값 -> 헥스를 쓰는 이유 => 가장 범용적인 방식 (문자열 색을 표현하기에 가장 무난)
        // ㄴ 1. 표준이고 무난함
        // ㄴ 2. 문자열 => 로그 포멧을 만들때 바로 끼워 넣기 좋음
        // ㄴ 3. 16진수 => 압축이 잘됨 (RGB)
        // - RGB (0, 0, 0) => 0 ~ 255 => FF = 255 / 00 = 0
        // EX : #FF0000 / #00FF00 / #FFFFFF
        // 구글 => 헥스코드 색상표

        if (!Enable)
        {
            return;
        }

        // 접두사 만들기 => tag가 있으면 해당 프리픽스를 만든다
        // 단, tag가 null / 빈 문자열이면 접두사 없이 msg만 출력
        string prefix = string.Empty;

        if (!string.IsNullOrEmpty(tag))
        {
            // t / colorHex => tag 부분만 색을 입히겠다 => 가독성
            if (EnableRichText && !string.IsNullOrEmpty(colorHex))
            {
                prefix = $"<color={colorHex}> [{tag}] </color>";
            }
            else
            {
                // (리치 텍스트를 사용하거나) 색상이 없다면 기본 형태로 만든다
                // 공백이 있어야 msg랑 안 붙는다
                prefix = $"[{tag}]";
            }
        }

        string final = $"{Indent}{prefix}{msg}";

        switch (kind)
        {
            case EnumELogKind.Log:
                Debug.Log(final, context);
                break;
            case EnumELogKind.Warn:
                Debug.LogWarning(final, context);
                break;
            case EnumELogKind.Error:
                Debug.LogError(final, context);
                break;
            case EnumELogKind.Success:
                Debug.Log(final, context);
                break;
        }
    }

    public static void IndentPop()
    {
        // 단계 내리기
        _indentLevel--;

        if (_indentLevel < 0)
        {
            _indentLevel = 0;
        }
    }

    public static void IndentReset()
    {
        _indentLevel = 0;
    }

    public static void Title(string title, char lineCh = '=')
    {
        Line(lineCh);
        Emit(EnumELogKind.Log, title);
        Line(lineCh);
    }
    public static void Section(string section, char lineCh = '-')
    {
        Emit(EnumELogKind.Log, section);
        Line(lineCh);

    }
    public static void Line(char ch = '-', int count = 10)
    {
        Emit(EnumELogKind.Log, new string(ch, count));
    }
    public static void Blank(int lines = 1)
    {
        // 콘솔에 빈줄만 추가

        // 빈줄 여러 줄
        if (lines <= 0)
        {
            return;
        }

        Debug.Log(new string('\n', lines));
    }

    // Log / Warn / Error
    public static void Log(string msg, UnityEngine.Object context = null)
    {
        Emit(EnumELogKind.Log, msg, null, null, context);
    }
    public static void Warn(string msg, UnityEngine.Object context = null)
    {
        Emit(EnumELogKind.Warn, msg, "WARN", "#FF9100", context);
    }
    public static void Error(string msg, UnityEngine.Object context = null)
    {
        Emit(EnumELogKind.Error, msg, "ERROR", "#FF1744", context);
    }

    public static void Success(string msg, UnityEngine.Object context = null)
    {
        Emit(EnumELogKind.Success, msg, "OK", "#00C8853", context);
    }

    public static void Assert(bool condition, string msg)
    {
        if (condition)
        {
            return;
        }

        Error($"[ASSERT] {msg}");
    }

    public static void CheckNull(object obj, string msg)
    {
        if (obj != null)
        {
            return;
        }

        Warn($"[NULL] {msg}");
    }

    [Obsolete("사용안함", false)]
    public static void Ref(string label, UnityEngine.Object obj)
    {
        // 유니티에 연결됐는지 가장 빠르게 확인할 수 있는 로그
        //  ㄴ null 경고
        //  ㄴ 아니면 이름을 출력

        // 유니티 null이 참 변신 괴물 같은 존재

        if (obj == null)
        {
            Warn($"{label} : <null> (인스펙터 연결 필요)");
            return;
        }

        Log($"{label} : {obj.name}");
    }

    public static T Ref<T>(T obj, string msg) where T : class
    {
        if (obj == null)
        {
            Warn($"[NULL] {msg}");
        }

        return obj;
    }

    public static void V3(string label, Vector3 v, int digits = 2)
    {
        // 숫자 자릿수를 줄여서 로그를 읽기 쉽게 만든다

        float x = (float)System.Math.Round(v.x, digits);
        float y = (float)System.Math.Round(v.y, digits);
        float z = (float)System.Math.Round(v.z, digits);

        Emit(EnumELogKind.Log, $"{label} : ({x}, {y}, {z})");
    }

    public static void KV(string key, object value)
    {
        // KV = key = value 형태로 값을 찍는 표준 포맷 헬퍼
        // 우리가 디버깅할때 => 가장 자주 찍는 형태 => 여기서 포멧 통일하겠다

        /*
         EX :
         CPrint.Group("Spawn Check", () => {
             CPrint.KV("PlayerPos", transform.position);
             CPrint.KV("HP", hp);
        });

        */
        Log($"{key} = {value}");
    }

    // 규모가 커진다 => 섹션을 만든다 (로그 덩어리)
    public static void Group(string title, Action body, char lineCh = '=', int lineCount = 20)
    {
        if (!Enable)
        {
            return;
        }

        // 타이틀 찍고 / 들여쓰기 올리고 / 그 안에 본문 실행하고 / 들여쓰기 내리고 / 구분선으로 마무리

        Title(title, lineCh);
        IndentPush();
        body?.Invoke();
        IndentPop();
        Line(lineCh, lineCount);
    }

    public static void Once(string key, string msg)
    {
        if (!Enable)
        {
            return;
        }

        if (_onceSet.Contains(key))
        {
            return;
        }

        _onceSet.Add(key);

        Warn($"[ONCE] {msg}");

        /*
         EX :
         CPrint.Once("NoRB","Rigidbody가 없어 물리 이동이 안됨 (혹은 동작하지 않음)");

        */
    }

    public static void OnceClear()
    {
        // 등록된 키 전부 비운다
        // ㄴ 보통 씬 재시작 => 테스트 반복 환경에서 사용할 수 있음
        _onceSet.Clear();
    }

    // 에디터 / 개발 빌드에서만 남기고 싶은 함수 모음
    //  ㄴ 규모가 적으면 속성으로 처리를 하고 => 함수가 많아지면 => 선택적 컴파일로 처리하면 된다
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Ray(Vector3 origin, Vector3 direction, Color color, float duration = 0f)
    {
        if (!Enable)
        {
            return;
        }

        Debug.DrawRay(origin, direction, color, duration);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Line3D(Vector3 a, Vector3 b, Color color, float duration = 0f)
    {
        if (!Enable)
        {
            return;
        }

        Debug.DrawLine(a, b, color, duration);
    }

    // 이후에는 필요하면 추가 예정
}