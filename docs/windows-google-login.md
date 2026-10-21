# Windows 구글 로그인 확인

## 현재 구현

`LoginController`에서 Windows용 `GoogleDesktopLogin`을 호출한다. 기본 브라우저로 구글 인증을 진행하고, PKCE / state를 사용해 같은 PC의 루프백 주소로 인증 코드를 받은 뒤 HTTPS로 ID 토큰과 교환한다.

| 코드 | 역할 |
| --- | --- |
| `LoginController` | 버튼 상태 / 화면 문구 / 취소 / 후속 서버 인증 호출 |
| `GoogleDesktopLogin` | 외부 JSON 설정 읽기 / PKCE 생성 / 브라우저 열기 / 토큰 교환 |
| `LoopbackCodeReceiver` | `127.0.0.1` 임시 포트에서 콜백 수신 / state 확인 / 브라우저 응답 |

구글 토큰은 파일이나 로그에 기록하지 않는다. Unity에서 ID 토큰을 수신했다는 사실만으로 사용자 인증을 완료하거나 DB에 접근하지 않는다. ID 토큰 검증 / DB 사용자 생성과 조회 / TCP 사용자 인증은 후속 서버 기능이다.

## Inspector 연결

1. `LoginScene`을 열고 `LoginController` 컴포넌트를 선택한다.
2. `Google Login Button`에 로그인 버튼, `Status Text`에 상태 문구용 TMP 텍스트를 지정한다.
3. `Google Client Json Path`에 Google Cloud에서 다운로드한 **데스크톱 앱 유형 JSON의 전체 파일 경로**를 입력한다. 경로 앞뒤의 큰따옴표는 제외한다.
4. 버튼의 `On Click()`에 `LoginController.OnGoogleLoginClicked()`를 한 번만 등록한다.
5. 씬에 활성 `EventSystem`과 입력 모듈이 있는지 확인한다.

이 JSON은 개발 PC의 저장소 밖에 보관한다. 파일 내용이나 시크릿을 Unity 에셋에 복사하지 않는다. 현재는 개발 PC의 외부 파일 경로로 확인하는 구성으로, Windows 배포용 설정 방식과 Android 로그인은 별도 구현이 필요하다. Windows Editor에서는 활성 빌드 대상이 Android여도 Windows 로그인 경로를 사용한다.

## 확인할 동작

- 버튼을 누르면 기본 브라우저가 열리고, 처리 중에는 버튼이 비활성화된다.
- 테스트 사용자로 등록한 구글 계정으로 로그인한다.
- 브라우저에 인증 코드 수신 안내가 나오면 Unity 창으로 돌아온다.
- 토큰 교환까지 끝나면 `구글 ID 토큰 수신 완료 / 서버 인증은 아직 연결되지 않았습니다.`가 표시된다.
- 이 단계에서 로비로 이동하지 않고 버튼이 다시 활성화된다. 다른 계정 선택이나 재시도를 할 수 있다.
- 인증 거절 / 화면 종료 / 5분 대기 시간 초과 시 작업을 종료하고 버튼을 복구한다. 브라우저 창만 닫는 경우에는 앱에서 이를 바로 알 수 없어 대기 시간 만료까지 기다린다.
- 잘못된 JSON 경로, 웹 앱 유형 JSON, 네트워크 오류 시 성공 상태로 넘어가지 않는지 확인한다.
- Android 실행에서는 지원하지 않는 플랫폼이라는 안내가 표시되는지 확인한다.

브라우저의 인증 코드 수신 문구는 토큰 교환 성공을 의미하지 않는다. Unity 상태 문구로 이번 단계의 완료 여부를 확인한다.

`Initialize`는 후속 처리를 위한 `Func<string, CancellationToken, UniTask>`를 받는다. 첫 인자는 받은 ID 토큰이다. HTTPS 서버 검증 / DB 사용자 처리 / TCP 연결과 인증까지 완료한 작업을 연결해야 하며, 그 작업이 성공했을 때만 `LobbyScene`으로 이동한다. 현재 프로젝트에는 이 후속 작업을 연결하는 호출부가 없다.

## 검증 범위

브랜치와 코드 / 기존 호출부를 확인했다. 빌드 / 테스트 / Play Mode / 실제 구글 로그인은 실행하지 않았다. 위 동작은 사용자가 Editor에서 확인해야 한다.

## 참고

- [구글 데스크톱 OAuth / PKCE / 루프백 콜백](https://developers.google.com/identity/protocols/oauth2/native-app)
- [서버에서 ID 토큰 검증](https://developers.google.com/identity/sign-in/web/backend-auth)
