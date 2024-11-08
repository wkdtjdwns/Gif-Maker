using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GifMakerApp
{
    public partial class Form1 : Form
    {
        // 이미지 관련 변수
        string spritesDir;
        string[] spritesPaths;
        int curFrameCnt = 0;
        Bitmap[] imgSprites;
        PictureBox mainFrame;
        Graphics screen;
        Size screenSize;

        // 화면 관련 변수
        bool isClick = false;
        int mode = 0;
        Dictionary<int, Pen> borderColor = new Dictionary<int, Pen>();
        Point startMousePosition = new Point();
        Size startFrameSize;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadResource();
            SetBorderColor();
            Setting();
        }

        // Resource 폴더에 있는 파일들을 긁어오는 메서드
        private void LoadResource()
        {
            spritesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\Resources");   // Resources 폴더의 위치 추적
            spritesPaths = Directory.GetFiles(spritesDir);                                          // 해당 폴더 안에 있는 파일들의 위치 저장
            imgSprites = new Bitmap[spritesPaths.Length];                                           // 파일의 개수만큼 이미지 배열 길이 초기화

            // 이미지 배열에 이미지 초기화
            for (int i = 0; i < imgSprites.Length; i++)
            {
                imgSprites[i] = new Bitmap(spritesPaths[i]);
            }
        }

        // 창의 테두리 색상을 설정하는 메서드
        private void SetBorderColor()
        {
            // borderColor 딕셔너리 값 초기화
            borderColor.Add(1, new Pen(Color.Blue, 7.5f));
            borderColor.Add(2, new Pen(Color.Red, 7.5f));
        }

        // 기본적인 화면 세팅을 담당하는 메서드
        private void Setting()
        {
            Size imgSize = imgSprites[0].Size;                  // 이미지들의 크기 저장 (어짜피 파일들의 크기는 달라지지 않을 것임)
            SetClientSizeCore(imgSize.Width, imgSize.Height);   // 폼의 클라이언트 영역 (타이틀 바를 제외한 부분: 실제 콘텐츠 영역) 크기 설정
            screenSize = imgSize;                               // 화면 크기 설정

            // PicturBox 객체에 각종 이벤트 추가 / PicturBox: 이미지, 비트맵 등을 표시하는 데 사용하는 Windows Forms의 컨트롤
            mainFrame = new PictureBox();
            mainFrame.Size = imgSize;
            mainFrame.MouseDown += Event_SetMousePosition;  // 마우스를 눌렀을 때
            mainFrame.MouseMove += Event_SetWindowPosition; // 마우스를 드래그 할 때
            mainFrame.MouseMove += Event_SetFormSize;       // 마우스를 드래그 할 때
            mainFrame.MouseUp += Event_CompleteSetFormSize; // 마우스를 땔 때
            mainFrame.Paint += Event_ChangeMode;            // Paint 컨트롤이 그려질 때

            // 이벤트들을 추가한 PicturBox 객체를 컨트롤에 추가
            Controls.Add(mainFrame);

            mainFrame.Image = imgSprites[0]; // 이미지 초기화

            // 프레임을 움직이는 함수를 비동기 (병렬) 실행
            Task t1 = new Task(new Action(PlayFrames));
            t1.Start();
        }

        // 마우스의 위치를 정하는 메서드
        private void Event_SetMousePosition(object args, MouseEventArgs e)
        {
            isClick = true; // MouseDown 이벤트에 해당 메서드를 넣을 것이기 때문에 마우스 클릭 여부 활성화

            startMousePosition = e.Location;    // 시작 마우스 위치 설정
            startFrameSize = this.Size;         // 시작 프레임 크기 설정
        }

        // 마우스의 위치에 따라 창을 이동시키는 메서드
        private void Event_SetWindowPosition(object args, MouseEventArgs e)
        {
            // 마우스 클릭 및 mode = 1일 때에만 실행 (하나라도 아니면 return)
            if (!isClick || mode != 1)
            {
                return;
            }

            // 위치를 마우스의 위치로 잡음
            Location = new Point(Cursor.Position.X - startMousePosition.X, Cursor.Position.Y - startMousePosition.Y);
        }

        // 마우스 드래그 방향에 따라 창의 크기를 변경하는 메서드
        private void Event_SetFormSize(object args, MouseEventArgs e)
        {
            // 마우스 클릭 및 mode = 2일 때에만 실행 (하나라도 아니면 return)
            if (!isClick || mode != 2)
            {
                return;
            }

            // 창 크기를 잡음
            screenSize = new Size(startFrameSize.Width + (e.Location.X - startMousePosition.X),
                                startFrameSize.Height + (e.Location.Y - startMousePosition.Y));

            // 가로 세로가 둘 다 0보다 클 때 (최소 크기 이상)
            if (screenSize.Width > 0 && screenSize.Height > 0)
            {
                // 위에서 잡은 창 크기 적용
                SetClientSizeCore(screenSize.Width, screenSize.Height);
                mainFrame.Size = screenSize;
            }
        }

        // 마우스를 땠을 때 창의 위치를 확정하는 메서드
        private void Event_CompleteSetFormSize(object args, MouseEventArgs e)
        {
            isClick = false;    // MouseUp 이벤트에 해당 메서드를 넣을 것이기 때문에 마우스 클릭 여부 비활성화

            ResetImg();         // 이미지 초기화
        }

        // 이미지를 초기화 하는 메서드
        private void ResetImg()
        {
            // 이미지와 이미지의 크기 설정
            for (int i = 0; i < imgSprites.Length; i++)
            {
                imgSprites[i] = new Bitmap(new Bitmap(spritesPaths[i]), screenSize);
            }

            // 설정한 이미지와 크기 적용
            mainFrame.Image = imgSprites[curFrameCnt];
        }

        // 모드를 전환하는 메서드
        private void Event_ChangeMode(object args, PaintEventArgs e)
        {
            // mode가 0보다 클 때 (0은 기본 모드)
            if (mode > 0)
            {
                // 이미지 창을 선택하고, 모드에 따라 테두리 색상 변경
                screen = e.Graphics;
                screen.DrawRectangle(borderColor[mode], new Rectangle(0, 0, Width - 1, Height - 1));
            }
        }

        // 이미지를 gif 파일처럼 이어주는 메서드 / async = 비동기 함수
        private async void PlayFrames()
        {
            // 무한 반복
            while (true)
            {
                // 이미지 개수만큼 반복
                for (curFrameCnt = 0; curFrameCnt < imgSprites.Length; curFrameCnt++)
                {
                    await Task.Delay(100);                      // 100ms 대기
                    mainFrame.Image = imgSprites[curFrameCnt];  // 다음 이미지 적용
                }
            }
        }

        // Key를 눌렀을 때 실행될 이벤트 메서드
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // 마우스 클릭 활성화일 때는 실행 안 함
            if (isClick)
            {
                return;
            }

            // 누른 Key에 따라 실행하는 로직 변경
            switch (e.KeyCode)
            {
                // Enter를 눌렀을 때 mode 변경
                case Keys.Enter:
                    mode++;
                    if (mode >= 3)
                    {
                        mode = 0;
                    }
                    break;

                // esc를 눌렀을 때 프로그램 종료
                case Keys.Escape:
                    Application.Exit();
                    break;
            }

            // 이미지 초기화
            ResetImg();
        }
    }
}
