# YuJanggi.Unity

Unity 기반 장기 클라이언트입니다. Local / AI 대국과 Network 대국의 준비·임시 이동 흐름을 제공하며, 게임 규칙과 상태는 `YuJanggi.Engine`, 서버 통신 계약은 `YuJanggi.Protocol`을 사용합니다.

> Network는 요청·응답·이동 이벤트 경로까지 구현되어 있습니다. 서버의 Engine 기반 이동 검증은 아직 미완료입니다.

## 1. 프로젝트 소개

- 클라이언트 담당: 로비 옵션·매칭 UI, 입력 해석, 대국·리플레이 진행, 장기판과 기물 표현
- 공용 패키지: Engine의 규칙·상태 조회 및 실행, Protocol의 메시지·직렬화·프레이밍
- 서버 연결: 별도 C#/.NET 프로젝트인 `YuJanggi.Server.V2`와 TCP 통신

## 2. 전체 구조

아래는 폴더 트리가 아닌 주요 실행 계층의 관계입니다.

```text
YuJanggi.Unity
├─ Lobby
│  ├─ View → Local / AI LobbyFlow → LobbyManager → JanggiScene
│  └─ View → NetworkLobbyFlow ↔ LobbyNetworkHandler ↔ Server
│                 └─ 준비 완료 → LobbyManager → JanggiScene
├─ InGame
│  └─ Controller → InGameFlow
│                    ├─ Local / AI → GameSession → Engine
│                    └─ Network ↔ InGameHandler ↔ Server
│                          └─ 이동 Event 수신 → GameSession → Engine
└─ Network
   └─ TcpTransport → NetworkConnection → NetworkManager
                                           ├─ Response → RequestDispatcher
                                           └─ Event → 기능별 NetworkHandler

공용 패키지
├─ YuJanggi.Engine   : 장기 규칙 / Board / Turn / Record / Score
└─ YuJanggi.Protocol : Client·Server 계약 / Serialization / Framing
```

Engine과 Protocol은 Unity 저장소 내부의 별도 구현이 아니라 외부 UPM 패키지입니다. Server도 두 패키지를 참조하지만, 현재 서버 이동 처리에는 Engine 규칙 검증이 연결되어 있지 않습니다.

## 3. 주요 계층 책임

| 계층 | 책임 |
| --- | --- |
| Lobby View | 옵션·포진 선택과 버튼 입력을 이벤트로 전달하고 연결·매칭 상태 표시 |
| LobbyFlow | Local / AI / Network별 준비 절차와 View 이벤트 수명 관리, `GameStartReady` 알림 |
| InGame Controller | 사용자 입력 또는 AI 결과를 선택·이동 의도로 전달. Engine 조회 계약 사용 |
| InGameFlow | Local의 즉시 이동과 Network의 요청·이벤트 적용 경로 구분 |
| GameSession | SessionState를 통한 대국·리플레이·종료 상태 관리, Engine·View·Controller 이벤트 연결 |
| NetworkHandler | 공통 전송 API 제공. LobbyNetworkHandler / InGameHandler에서 기능별 Protocol 해석·송수신 |
| RequestDispatcher | RequestId로 응답 대기를 연결하고, PendingRequestTracker와 요청 완료·취소·정리 처리 |
| NetworkConnection | 연결·Handshake·수신 루프와 연결 수명 관리 |
| TcpTransport | TCP 송수신, Protocol 직렬화·프레이밍 호출, 메시지 길이만큼 읽기와 송신 직렬화 |
| YuJanggi.Engine | Unity에 의존하지 않는 장기 규칙과 한 대국의 상태·진행 |
| YuJanggi.Protocol | 메시지 종류·DTO·RequestId 계약, JSON 직렬화와 길이 기반 프레이밍 |

`LobbyManager`는 Flow 구성·패널 전환·최종 옵션 저장·씬 진입을, `InGameManager`는 Engine·Session·View·Flow 구성과 생명주기를 담당합니다. `NetworkManager`는 수신 메시지를 분배하고 UI용 연결·매칭 상태를 모읍니다.

## 4. 게임 모드

| 모드 | Lobby Flow | InGame Flow | 진행 방식 |
| --- | --- | --- | --- |
| Local | LocalLobbyFlow | LocalInGameFlow | 한 클라이언트에서 두 진영 입력, 서버 불필요 |
| AI | AILobbyFlow | LocalInGameFlow | 로컬 입력과 AI Controller의 이동 결과, 서버 불필요 |
| Network | NetworkLobbyFlow | NetworkInGameFlow | 서버 매칭·시작 이벤트 대기, 이동 Event 이후 반영 |

Network의 내 입력도 Local과 같은 입력 Controller를 사용합니다. 서버 전송 여부는 Controller가 아닌 Flow가 결정합니다.

## 5. Lobby 흐름

```text
Local View → LocalLobbyFlow ─┐
                            ├→ GameStartReady → LobbyManager → JanggiScene
AI View    → AILobbyFlow ────┘

Network View → NetworkLobbyFlow → LobbyNetworkHandler → Server
Server → GameReadyEvent → LobbyNetworkHandler → NetworkLobbyFlow
       → GameStartReady → LobbyManager → JanggiScene
```

세 Flow는 `LocalFlow`를 상속해 구독·해제와 준비 완료 알림을 공유합니다. `LobbyManager`는 `LobbyGameStartContext`의 옵션·AI 전략·매치 정보를 저장한 뒤 씬을 전환합니다.

온라인 준비 순서:

```text
연결·Handshake
→ MatchingStartRequest / MatchingStartResponse
→ MatchingFoundEvent
→ FormationSubmit (양측 포진 제출)
→ GameReadyEvent (확정 포진)
→ JanggiScene
```

매칭 신청·취소는 Response를 기다립니다. `FormationSubmit`은 단방향 전송이며, 최종 준비는 `GameReadyEvent`로 확인합니다. Handler는 Protocol을 처리하고 `LobbyNetworkService`는 매칭·포진 상태를 관리합니다.

## 6. InGame 흐름

Local / AI는 Flow 진입 시 바로 대국을 시작합니다. Network는 씬 준비와 대국 시작을 구분합니다.

```text
NetworkInGameFlow → GameSceneReady → Server의 양측 준비 확인
Server → GameStartEvent → InGameHandler → NetworkInGameFlow
       → GameSession.StartGame()
```

기물 이동 경로:

```text
[Local / AI]
사용자 입력 / AI 결과 → Controller → LocalInGameFlow → GameSession → Engine

[Network 요청]
사용자 입력 → Controller → NetworkInGameFlow → InGameHandler
           → MovePieceRequest → Server
Server → MovePieceResponse → RequestDispatcher → 요청 대기 완료

[Network 이동 반영: 내 이동과 상대 이동의 공통 경로]
Server → MovePieceEvent → InGameHandler → NetworkInGameFlow
       → GameSession → SessionState → Engine
```

`MovePieceResponse`의 Accepted만으로 Client Engine을 변경하지 않습니다. `MovePieceEvent`를 받은 뒤 Main Thread에서 현재 흐름과 턴을 확인해 적용합니다. Live뿐 아니라 Replay 상태에도 수신 이동을 전달하며, Engine 이벤트는 Session을 통해 화면·턴 진행에 반영됩니다.

## 7. Network 구조

Handshake 이후 서버 메시지를 받는 경로입니다.

```text
TCP → TcpTransport → NetworkConnection → NetworkManager
                                           │
                         RequestId 있음 ────┤→ RequestDispatcher
                                           │   └→ 해당 요청의 await 완료
                         RequestId 없음 ────┘→ 메시지 종류별 Handler
                                               ├→ LobbyNetworkHandler
                                               └→ InGameHandler
```

- **Request / Response:** 요청 ID와 예상 응답 종류로 대기 작업을 연결합니다. 매칭 신청·취소와 이동 요청에 사용하며, 완료 후 대기를 정리하고 연결 종료 시 취소합니다.
- **Event:** 특정 요청의 응답이 아닌 서버 알림입니다. `MatchingFoundEvent`, `GameReadyEvent`, `GameStartEvent`, `MovePieceEvent`를 기능별 Handler로 전달합니다.
- **단방향 송신:** `FormationSubmit`, `GameSceneReady`는 RequestId 없이 전송하며 대응 Response를 기다리지 않습니다.

Handshake 요청·응답은 일반 수신 루프를 시작하기 전에 `NetworkConnection`에서 직접 처리합니다. `NetworkHandler`의 요청 전송은 Dispatcher를, 응답 대기가 없는 전송은 Connection을 사용합니다.

## 8. Shared Packages

### YuJanggi.Engine

장기 규칙과 Board / Turn / Record / Score를 관리하는 Unity 비의존 C# 패키지입니다. Session은 게임 진행 API를, Controller와 View는 필요한 조회·읽기 전용 계약을 사용합니다.

### YuJanggi.Protocol

Client / Server 메시지 종류, DTO, RequestId 계약을 정의합니다. `MessageSerializer`는 JSON 직렬화, `MessageFramer`는 길이 헤더를 이용한 메시지 경계 처리를 담당하며 실제 소켓은 클라이언트·서버가 관리합니다.

현재 [manifest.json](Packages/manifest.json)은 다음 **로컬 UPM 경로**를 참조합니다.

| 패키지 | manifest 참조 | 참조 대상 package.json 버전 |
| --- | --- | --- |
| YuJanggi.Engine | `file:D:/Git/YuJanggi/YuJanggi.Engine/upm` | 2.4.0 |
| YuJanggi.Protocol | `file:D:/Git/YuJanggi/YuJanggi.Protocol/upm` | 1.2.0 |

manifest 자체에 버전을 고정한 방식은 아닙니다. 위 버전은 현재 경로의 패키지 메타데이터 기준이며, 다른 환경에서는 두 경로를 실제 패키지 위치로 맞춰야 합니다.

## 9. 주요 디렉터리

```text
Assets/Scripts
├─ Bootstrap             # 초기화, NetworkManager·AudioManager
├─ Lobby
│  ├─ Flow               # 모드별 대국 준비
│  └─ LobbyNetwork       # 로비 통신 API·Handler·상태 Service
├─ InGame
│  ├─ Controller         # Local·AI 입력 및 Remote 역할
│  ├─ Flow               # Local·Network 시작과 이동 경로
│  ├─ Handler            # 인게임 메시지 처리
│  ├─ Service            # 게임 시작 상태·대기
│  ├─ Session            # SessionState와 게임 진행
│  └─ Views              # Live·Replay 표현
├─ Network               # Transport·Connection·요청 추적·공통 Handler
└─ Runtime               # Input·Board·Piece·Particle·UI
```

흐름의 진입점: [LobbyManager](Assets/Scripts/Lobby/LobbyManager.cs) · [InGameManager](Assets/Scripts/InGame/InGameManager.cs) · [NetworkManager](Assets/Scripts/Bootstrap/NetworkManager.cs)

## 10. 현재 구현 상태

### 구현

- Local 2인 대국과 AI 대국
- 기물 선택·이동 가능 위치 표시, 대국 진행과 기록 기반 리플레이
- TCP 연결과 Engine / Protocol 버전 Handshake
- 매칭 신청·취소, 포진 제출과 GameReady에 따른 씬 진입
- 양측 GameSceneReady 이후 GameStartEvent에 따른 대국 시작
- MovePieceRequest / Response / Event를 통한 임시 온라인 이동 경로
- RequestId 기반 요청·응답 연결과 연결 종료 시 대기 정리

### 미완료 / 제한

- **서버 Engine 기반 이동 검증:** 현재 Server.V2의 `ValidateMove`는 항상 Accepted를 반환합니다. 턴·기물 소유·합법 이동을 서버가 판정하는 구조는 미완료입니다.
- **재접속·네트워크 Snapshot 복구:** 현재 메시지 계약과 클라이언트 흐름에 대국 상태 복원 경로가 없습니다.
- **온라인 이동 외 명령 동기화:** 무르기·기권·한 수 쉼 등은 현재 Session에서 Engine을 호출하며, 별도의 서버 요청·이벤트 계약이 없습니다.
- **온라인 제한시간:** 클라이언트 옵션은 현재 30초 기본값을 사용하며 서버 TurnTime 계약은 없습니다.

코드·설정을 정적으로 확인한 구현 범위입니다. 이 README 갱신 과정에서 빌드·테스트·Unity·서버를 실행하지 않았습니다.

## 11. 실행 / 의존성

### 요구 사항

- **Unity 6000.3.1f1** — [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt) 기준
- **공용 패키지** — 위 Engine / Protocol UPM 경로의 실제 패키지 필요
- **주요 UPM 의존성** — UniTask(Git 참조), Input System 1.17.0, URP 17.3.0, uGUI 2.0.0. 전체 설정은 [manifest.json](Packages/manifest.json) 참조
- **프로젝트 포함 플러그인** — DOTween, `Assets/Plugins/YuJanggiCommon`의 System.Text.Json 관련 DLL. Protocol 패키지는 System.Text.Json 8.0.5와 의존 라이브러리를 요구합니다.
- **UI** — TextMeshPro 사용

### 시작 순서

1. Engine / Protocol의 로컬 UPM 경로를 확인하고 Unity 6000.3.1f1로 프로젝트를 엽니다.
2. [BootStrapScene](Assets/Scenes/BootStrapScene.unity)을 열고 Play합니다. Network·Audio 초기화 후 `LobbyScene`으로 이동합니다.
3. 로컬 또는 AI 모드를 선택하면 서버 없이 대국을 시작합니다.
4. Network는 별도 `YuJanggi.Server.V2`가 필요합니다. 서버와 클라이언트의 Engine / Protocol 버전이 Handshake 검사를 통과해야 합니다.
5. 두 클라이언트가 같은 서버에 연결해 매칭·포진 제출·씬 준비를 마치면 대국 시작 이벤트를 받습니다.

현재 BootStrapScene의 서버 설정은 **127.0.0.1:7777**입니다. 다른 호스트를 사용할 경우 씬의 `YuJanggiBootStrap`에 직렬화된 `_host`, `_port`를 변경해야 합니다. 활성 씬 순서는 `BootStrapScene → LobbyScene → JanggiScene`입니다.

서버 빌드·배포 절차는 서버 저장소에서 다룹니다. Engine 분리 과정, Flow 리팩터링 배경, 요청 수명·Replay 문제 해결 회고는 별도 포트폴리오에서 다룹니다.
