# YuJanggi.Unity

Unity 6 기반 한국 장기 게임입니다.

로컬 대국, AI 대국, 기보 리플레이를 구현하며 게임 규칙과 화면의 분리, 상태 전환, 비동기 AI 처리에 집중했습니다.

[포트폴리오](https://app.notion.com/p/3b28a299d1c480ed867fef02568ca410) / [실행 파일](https://app.notion.com/p/38e8a299d1c48043b6a8f045695abf57) / [Core](https://github.com/SeokJinYoo98/YuJanggi.Core) / [Server](https://github.com/SeokJinYoo98/YuJanggi.Server)

## 주요 기능

- **대국:** 로컬 대국과 AI 대국, 한 수 쉼, 무르기, 기권

- **AI:** Random, Greedy, Minimax 전략

- **리플레이:** 진행 중과 종료 후 기보 탐색, Live 복귀

- **입력과 표현:** 마우스와 터치, 이동 가이드, 애니메이션, 사운드

## 설계에서 집중한 점

- **규칙과 화면 분리:** Controller는 이동을 요청하고 Core가 검증합니다. View는 결과를 표현해 입력 방식과 규칙 구현을 분리했습니다.

- **Live와 Replay 분리:** 진행 중인 대국을 유지하면서 과거 기보를 볼 수 있도록 상태를 나눴습니다. Live 복귀 시 최신 보드로 화면을 맞춥니다.

- **비동기 AI:** 복사한 보드에서 탐색을 수행해 실제 대국 보드와 분리했습니다. 턴 종료 시 취소를 요청하고, 선택한 수는 Core에서 다시 검증합니다.

- **반복 오브젝트 재사용:** 이동 가이드와 파티클은 Object Pool로 관리하고, 기물 데이터는 ScriptableObject로 분리했습니다.

ReplayView는 실제 대국 보드를 유지하며 기보 커서와 화면을 갱신합니다.

Core는 Git UPM으로 참조합니다. Unity와 Server의 Core 커밋은 함께 맞춰야 합니다.

## 기술

- 엔진: Unity **6000.3.1f1**, URP **17.3**

- 입력: Input System **1.17**

- 비동기와 애니메이션: UniTask, DOTween

## 실행

1. 저장소를 Clone하고 Unity Hub에서 엽니다.

2. Unity **6000.3.1f1**에서 패키지 복원을 기다립니다.

3. Assets/Scenes/LobbyScene.unity를 열고 Play를 누릅니다.

## 현재 상태

- **구현:** 로컬, AI, 리플레이, TCP 송수신, 로비 참가, 매칭 응답 처리

- **연결 확인 완료:** Unity 클라이언트 2개를 실행해 서버 접속과 연결 해제를 확인했습니다. 랭크 버튼으로 연결과 취소를 전환합니다.

- **매칭 안내:** 랭크 버튼으로 접속 후 매칭을 요청합니다. 서버가 보낸 초와 한의 이름을 Debug.Log로 출력합니다. 기본 Player 이름에는 실행별 식별자를 붙입니다.

- **온라인 미완성:** 대국 씬 진입, 이동 요청과 서버 결과 반영

  NetworkController는 빈 구현입니다.

통신 코드: Assets/Scripts/Runtime/Network

공용 DLL: Assets/Plugins/YuJanggiCommon

## 사용 에셋

- 장기말: [장기 Janggi KOREA Ver](https://www.acon3d.com/ko/product/1000013872)

- UI와 배경: Aseprite로 직접 제작

- 사운드: [Pixabay](https://pixabay.com/sound-effects/search/chess/)
