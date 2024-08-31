# SCP: Secret Laboratory Chzzk Integration
## 사용법
1. 플러그인을 빌드합니다. (장난입니다. 태그 탭에 들어가 플러그인 파일을 다운받습니다. ~~Edit: 귀찮음~~)
2. 플러그인을 엑자일드 플러그인 폴더에 넣고 서버를 한 번 실행합니다.
3. 엑자일드 컨피그 파일에 ScpSlChzzkIntegration 항목이 생겼으면 그 안에 자신의 채널 ID를 적습니다.
3-1. 자신의 채널 ID는 채널 URL에서 가져올 수 있습니다. (https://chzzk.naver.com/5c565a8a9ceaf04f7c0a1909d71da44e 일 경우 5c565a8a9ceaf04f7c0a1909d71da44e)
4. RA에서 /settarget <자신의 닉네임 또는 id>을 사용해 후원 이벤트의 타겟을 설정합니다.


## 현재 가능한 후원 이벤트 목록
- 자신 위치에 폭탄 소환 (컨피그에서 시간 조절 가능) Bomb
- 랜덤 SCP 자신의 위치로 텔레포트 TeleportRandomScp
- 랜덤 아이템 삭제 DeleteRandomItem
- 인벤토리의 모든 아이템 드랍 DropAllItem
- 모든 플레이어를 자신의 위치로 텔레포트 BringAllPlayers
- 핵탄두 시작 StartWarhead
- 핵탄두 폭파 DetonateWarhead
- 자신이 있는 방 정전 (컨피그에서 시간 조절 가능) Blackout
- 자신이 있는 방 봉쇄 (컨피그에서 시간 조절 가능) Lockdown
- 플레이어의 움직임 일시정지 (컨피그에서 시간 조절 가능) Ensnared
- 랜덤 플레이어 자신에게 텔레포트 BringRandomPlayer

### Credits
유튜버 각별님의 영상 (https://youtu.be/LSzwtXM6WFo?si=ddTcJgPy0ICa-aYf) 에서 영감을 얻었습니다.

JoKangHyeon님의 코드를 상당히 참고했습니다. (https://github.com/JoKangHyeon/ChzzkUnity) ~~(솔직히 코드 복붙해서 SL로 포팅한 수준입니다)~~
