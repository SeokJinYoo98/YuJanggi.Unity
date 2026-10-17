<h3 align="center">Tech Stack</h3>

<p align="center">
  <img src="https://cdn.jsdelivr.net/gh/devicons/devicon@latest/icons/unity/unity-original.svg" height="40" alt="Unity" title="Unity" />
  <img src="https://cdn.jsdelivr.net/gh/devicons/devicon@latest/icons/csharp/csharp-original.svg" height="40" alt="C#" title="C#" />
</p>

<p align="center">
  Unity 6000.3.26f1 · UniTask · CancellationToken · DOTween<br />
  Input System · uGUI / TextMeshPro · ScriptableObject · Object Pool<br />
  TCP · UPM / npm · AudioMixer · JSON 설정 저장
</p>

# YuJanggi.Unity

로컬·AI·온라인 대국을 지원하는 Unity 장기 클라이언트입니다.<br />
Unity 독립 규칙 Engine과 공용 Protocol을 사용하며, AWS EC2의 .NET TCP 서버와 연동합니다.

## 프로젝트 소개 · 플레이 영상

한 클라이언트에서 두 진영을 조작하거나 AI와 대국하고, 네트워크 모드에서는 매칭·포진 선택 후 상대와 대국합니다.<br />
기보 재생, 리매치와 인게임 채팅을 제공합니다.

### 플레이 빌드 다운로드 · v1.0.2

| 플랫폼 | 다운로드 | 실행 방법 |
| --- | --- | --- |
| Android | [APK 다운로드](https://github.com/SeokJinYoo98/YuJanggi.Unity/releases/download/play-v1.0.2/YuJanggi_v1.0.2.apk) | Android 기기에 APK를 설치합니다. |
| Windows | [ZIP 다운로드](https://github.com/SeokJinYoo98/YuJanggi.Unity/releases/download/play-v1.0.2/YuJanggi.v1.0.2.zip) | 압축을 모두 해제한 뒤 `YuJanggi.exe`를 실행합니다. |

[플레이 빌드 Release](https://github.com/SeokJinYoo98/YuJanggi.Unity/releases/tag/play-v1.0.2) · [이전 Windows 설치파일](Downloads/YuJanggi_V1.0.0_Setup.exe?raw=true)

## 주요 기능

| 기능 | 구현 내용 |
| --- | --- |
| 대국 모드 | Local / AI / Network |
| AI 전략 | Random / Greedy / Minimax |
| 보드 입력 | 기물 선택 · 이동 가능 위치 표시 · 이동 요청 |
| 기보 재생 | Live / Replay 전환 · 기록 탐색 중 실제 대국 진행 유지 |
| 온라인 대국 | 매칭 · 포진 · 이동 · 기권 · 한 수 쉼 · 양측 요청 기반 리매치 |
| 채팅 | 서버 Event 수신 후 메시지 표시 · 엔터·버튼 전송 · 하단 스크롤 |
| 음량 설정 | Master / SFX / UI / BGM 조절 · JSON 저장·복원 |

## 설치 · 실행 방법

1. **Unity 6000.3.26f1**에서 프로젝트를 엽니다.
2. [manifest.json](Packages/manifest.json)에 설정된 의존성을 복원합니다.
3. [BootStrapScene](Assets/Scenes/BootStrapScene.unity)을 열고 Play합니다. 초기화 후 Lobby로 이동합니다.

Engine **2.8.4** / Protocol **1.7.6**은 npm Scoped Registry에서 설치합니다.<br />
레지스트리 URL은 `https://registry.npmjs.org`, Scope는 `com.yujanggi`이며 프로젝트에 설정되어 있습니다.

Local / AI는 서버 없이 실행합니다. Network 대국은 서버와 두 클라이언트가 필요하며, Engine / Protocol 버전이 Handshake 검사를 통과해야 합니다.

Unity 빌드는 [Build Profiles](Assets/Settings/Build%20Profiles)의 Debug / Release 프로필을 사용합니다.

## 클라이언트 구조

게임 진행과 화면 탐색을 분리해, 과거 기보를 보는 동안에도 실제 대국 상태를 갱신합니다.

| 구성 요소 | 역할 |
| --- | --- |
| `InGameManager` | Engine·GameMode·State 생성, 이벤트 연결과 생명주기 관리 |
| `GameMode` | Local / AI / Network별 명령 처리와 턴 입력 제어 |
| `InGameLiveState` / `InGameReplayState` | 실제 대국 표시와 과거 기보 탐색 |
| InputHandler / Player | 입력 수집, 기물 선택과 이동 요청 해석 |
| `InGameView` | 보드·기물·대국 UI 표시 |
| `ChattingPanel` / `MessageView` | 채팅 수신 구독, 발신 팀별 말풍선과 안내 표시 |

장기 규칙과 게임 상태는 `YuJanggi.Engine`, 통신 DTO와 패킷 프레이밍은 `YuJanggi.Protocol` 패키지에서 제공합니다.

## 서버 연동 흐름

클라이언트는 공용 Protocol로 AWS EC2의 TCP 서버와 통신합니다.

1. Lobby에서 연결·버전 Handshake·매칭·포진 준비를 진행합니다.
2. 양쪽 클라이언트의 게임 씬 준비가 끝나면 서버 시작 Event를 받아 대국을 시작합니다.
3. `RequestDispatcher`가 RequestId로 응답을 연결하고, `NetworkManager`가 서버 Event를 기능별 Handler로 전달합니다.
4. 온라인 이동·최종 종료 결과는 승인 응답과 구분해 서버 Event 수신 후 반영합니다.
5. 리매치는 양측 요청 후 확정 Event를 기다리고, 채팅도 서버가 전달한 Event를 받아 표시합니다.

UniTask와 CancellationToken으로 비동기 작업을 관리하며, 대국 명령·채팅 요청의 응답 대기 중 중복 전송을 제한합니다.

## 현재 구현 범위 · 향후 계획

- **구현**: 로컬·AI·온라인 대국, 기보 재생, 기권·한 수 쉼, 리매치, 채팅, 음량 설정 저장.
- **향후 계획**: 사용자 인증·DB 연동, 이탈 알림·재접속 복구, 모바일 대응.
- **서버 측 과제**: Engine 기반 이동·최종 결과 검증. 현재 온라인 이동 전달을 서버 권위 규칙 검증과 구분합니다.

Engine / Protocol의 GitHub Actions에서 UPM 패키지를 npm에 배포합니다.<br />
이 Unity 저장소에는 별도 CI/CD Workflow가 없습니다.

## 관련 저장소 · 문서

- [YuJanggi.Engine](https://github.com/SeokJinYoo98/YuJanggi.Engine) — Unity 독립 장기 규칙·게임 상태·기보
- [YuJanggi.Protocol](https://github.com/SeokJinYoo98/YuJanggi.Protocol) — 공용 메시지 계약·직렬화·패킷 프레이밍
- [YuJanggi.Server](https://github.com/SeokJinYoo98/YuJanggi.Server) — AWS EC2 기반 TCP 서버·매칭·GameRoom·리매치·채팅
