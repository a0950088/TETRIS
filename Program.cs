using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Timers;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace TETRIS_START {
    class Program {
        // 全域變數
        public static string IP = "127.0.0.1", User = "hi", Pwd = "1qaz@WSX"; // Database的資料
        public static int Cube_Now_Type = 1, Cube_Now_X = 28, Cube_Now_Y = 2; // 方塊的座標、型態 type、x、y
        public static int Position_Projection_X = 28, Position_Projection_Y = 19; // 投影的座標 x、y
        public static int Cube_Hold_X = 7, Cube_Hold_Y = 6, Cube_Next_X = 51, Cube_Next_Y = 6; // HOLD、NEXT的座標 x、y
        public static char[] Cube = {'O', 'I', 'Z', 'S', 'J', 'L', 'T'}; // 方塊的類型Array
        public static char[,]  Tetris_Array = new char[22, 42]; // 遊戲中紀錄方塊的Array
        public static char Cube_Now = RANDOM_CUBE(), Cube_Next = RANDOM_CUBE(), Cube_Hold = ' '; // NOW、NEXT、HOLE的方塊
        public static int Count = 0, Score = 0, Score_Add_Time = 0, Score_Eliminate_Time = -1; // 成績相關int變數
        public static int User_Highscore = 0, Sec = 1000, Level = 1, Line_Count = 0; // 成績相關int變數
        public static int[] HighScore = new int[20]; // 紀錄最高分的Array
        public static string[] HighScoreName = new string[20]; // 紀錄最高分UserName的Array
        public static bool Add_Cube = true/*當方塊放好，增加方塊*/, Clean_Flag = false, Judge_Flag = true, End = false, log_flag = false;
        public static string User_Account = null, User_Password = null ,User_Nickname = null; // 玩家資料
        private static System.Timers.Timer aTimer; //宣告一個static Timer 
        // 全域變數
        static void Main(string[] args) {            
            String connetStr = "server="+IP+";port=3306;user="+User+";password="+Pwd+"; database=tetris;";
            MySqlConnection conn = new MySqlConnection(connetStr);
            MySqlCommand command = conn.CreateCommand();
            string Buildaccount;

            SIGN_AND_LOG_SCREAN();
            Buildaccount = WELCOME();
            Console.Clear();
            //註冊系統
            if(Buildaccount == "N"){
                SIGN_AND_LOG_SCREAN();
                DATABASE_SIGN_ACCOUNT();
                DATABASE_SIGN_PASSWORD();
                DATABASE_SIGN_NICKNAME();
                DATABASE_SIGN();
                log_flag = true;
            }
            else if(Buildaccount == "Y"){
                log_flag = true;
            }
            //登入系統
            SIGN_AND_LOG_SCREAN();
            DATABASE_LOGIN();
//-----------------------------------------------------------------------------------------------------------
            CLEAR_ARRAY();
            START();
            ADD_CUBE();
            Add_Cube = false;
            
            while( !End ){
                // 先判斷投影是否碰到 叫出投影和方塊
                if(HIT_TOP()){
                    End = true;                    
                    if(User_Highscore < Score){
                        User_Highscore = Score;
                        conn.Open();
                        string sql = string.Empty;
                        sql = "UPDATE `userdata` SET `highscore` = '"+User_Highscore+"' WHERE `account` = '"+User_Account+"';";
                        MySqlCommand cmd = new MySqlCommand(sql,conn);
                        cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                    GAMEOVER();
                    if( !End ){
                        CLEAR_ARRAY();
                        Level = 1;Line_Count = 0;Score = 0;
                        Score_Add_Time = 0;Score_Eliminate_Time = -1; Sec = 1000;
                        START();
                        ADD_CUBE();
                        Add_Cube = false;
                    }
                    else{
                        continue;
                    }
                }
                if(Add_Cube){
                    ADD_CUBE();
                }
                if( Console.KeyAvailable & !End ){
                    aTimer = new System.Timers.Timer(10); // 每0.5秒後執行一次
                    aTimer.Elapsed += new ElapsedEventHandler(RUN);// 執行RUN這個事件  
                    aTimer.AutoReset = true;
                    aTimer.Enabled = true;
                    aTimer.Start(); // 開始執行
                }
                else{
                    // Level_1 每1秒往下掉落 GOTOXY y座標-1
                    if(Cube_Now_Y == Position_Projection_Y){
                        CUBE('+', Cube_Now, Cube_Now_Type, Cube_Now_X, Cube_Now_Y);
                        CUBE_DISAPPEAR();
                        Add_Cube = true;
                    }
                    else{
                        Add_Cube = false;
                        CUBE('-', Cube_Now, Cube_Now_Type, Cube_Now_X, Cube_Now_Y);
                        Cube_Now_Y++;
                        CUBE('+', Cube_Now, Cube_Now_Type, Cube_Now_X, Cube_Now_Y);
                    }
                }
                Thread.Sleep(Sec);
            }
        }
        // 開始與結束計時器
        public static void RUN(Object source, ElapsedEventArgs e){
            if(End){
                aTimer.Stop();
            }
            else{
                aTimer.Stop();
                Console.ForegroundColor = ConsoleColor.Black;
                ConsoleKeyInfo c = Console.ReadKey();
                KBHIT(c);
                aTimer.Start();
            }
        }
        // 登入畫面
        static string WELCOME(){
            string Buildaccount;
            GOTOXY(18,7);Console.Write("歡迎來到俄羅斯方塊Tetris!");
            GOTOXY(7,9);Console.Write("請問是否擁有Tetris帳號（有請輸入ｙ，無請輸入ｎ）?");
            while(true){
                GOTOXY(52,8);Buildaccount = Console.ReadLine();
                GOTOXY(8,9);Console.Write("                    ");
                if(Buildaccount != "Y" & Buildaccount != "N"){
                    Thread.Sleep(10);
                    GOTOXY(8,9);Console.Write("輸入錯誤！請輸入ｙ或ｎ");GOTOXY(52,8);Console.Write(" ");
                }
                else{
                    break;
                }
            }
            return Buildaccount;
        }
        static string INPUT_PASSWORD(string User_Password){
            string[] Password = new string[8];
            int i = 0;
            while(true){
                ConsoleKeyInfo Cki = Console.ReadKey(true);
                if(Cki.Key != ConsoleKey.Enter){
                    if(Cki.Key != ConsoleKey.Backspace){
                        Password[i] = Cki.KeyChar.ToString();
                        Console.Write("*");
                        i++;
                    }
                    else{
                        Password[i-1] = null;
                        Console.Write("\b \b");
                        i--;
                    }
                }
                else{
                    for(i=0; i<8; i++){
                        User_Password += Password[i];
                        Password[i] = null;
                    }
                    Console.WriteLine();
                    break;
                }
            }
            return User_Password;
        }
        // 登入畫面的邊框
        static void SIGN_AND_LOG_SCREAN(){
            Console.WriteLine("◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇                                                        ◇");
            Console.WriteLine("◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇◇");
            WORD_TETRIS(1);
            Console.BackgroundColor = ConsoleColor.Black;
        }
        // 判斷新出來的方塊是否超出頂端邊界
        static bool HIT_TOP(){
            bool Hit = false;
            for(int i=26; i<34; i++){
                if(Tetris_Array[1,i] != 'a' || Tetris_Array[2,i] != 'a'){
                    Hit = true;
                    break;
                }
            }
            return Hit;
        }
        // Randoom出新的方塊並列印在Next框格內
        static void ADD_CUBE(){
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Cube_Now_X = 28;
            Cube_Now_Y = 2;
            Position_Projection_X = 28;
            Cube_Now_Type = 1;
            Cube_Now = Cube_Next;
            Cube_Next = RANDOM_CUBE();
            CLEAR_NEXT();
            Position_Projection_Y = JUDGE_Projection_XY(Cube_Now,Cube_Now_Type,Position_Projection_X,Position_Projection_Y,Cube_Now_Y);//return Y
            CUBE_PROJECTION('+', Cube_Now, Cube_Now_Type, Position_Projection_X, Position_Projection_Y);
            CUBE('+', Cube_Now, Cube_Now_Type, Cube_Now_X, Cube_Now_Y);
            CUBE('+', Cube_Next, 1, Cube_Next_X, Cube_Next_Y);
        }
        // 清除儲存遊戲框格內方塊位置的陣列
        static void CLEAR_ARRAY(){
            for(int i=18; i<=41; i++){
                Tetris_Array[0, i] = 'w';
                Tetris_Array[21, i] = 'w';
            }
            for(int i=0; i<=21; i++){
                Tetris_Array[i, 18] = 'w';
                Tetris_Array[i, 19] = 'w';
                Tetris_Array[i, 40] = 'w';
                Tetris_Array[i, 41] = 'w';
            }
            for(int i=1; i<=20; i++){
                for(int j=20; j<=39; j++){
                    Tetris_Array[i, j] = 'a';
                }
            }
            GOTOXY(0, 0);
        }
        // 呼叫 Console.SetCursorPosition(X, Y) 的縮減版副程式
        static void GOTOXY(int X, int Y){
            Console.SetCursorPosition(X, Y);
        }
        // 判斷是否有按按鍵並做出對應的判斷與反應
        static void KBHIT(ConsoleKeyInfo c){
            switch(c.Key.ToString()){
                case "LeftArrow":// 往左移-->判斷是否到邊界、旁邊是否有方塊、投影位置
                    if( !HIT("Left", Cube_Now_Type) ){
                        break;
                    }
                    CUBE('-', Cube_Now, Cube_Now_Type, Cube_Now_X, Cube_Now_Y);
                    CUBE_PROJECTION('-', Cube_Now, Cube_Now_Type, Position_Projection_X, Position_Projection_Y);
                    Cube_Now_X -= 2;
                    Position_Projection_X = Cube_Now_X;
                    Position_Projection_Y = JUDGE_Projection_XY(Cube_Now,Cube_Now_Type,Position_Projection_X,Position_Projection_Y,Cube_Now_Y);
                    CUBE_PROJECTION('+', Cube_Now, Cube_Now_Type, Position_Projection_X, Position_Projection_Y);
                    CUBE('+', Cube_Now, Cube_Now_Type, Cube_Now_X, Cube_Now_Y);
                    Add_Cube = false;
                    break;
                case "RightArrow":// 往右移-->判斷是否到邊界、旁邊是否有方塊、投影位置
                    if( !HIT("Right", Cube_Now_Type) ){
                        break;
                    }
                    CUBE('-', Cube_Now, Cube_Now_Type, Cube_Now_X, Cube_Now_Y);
                    CUBE_PROJECTION('-', Cube_Now, Cube_Now_Type, Position_Projection_X, Position_Projection_Y);
                    Cube_Now_X += 2;
                    Position_Projection_X = Cube_Now_X;
                    Position_Projection_Y = JUDGE_Projection_XY(Cube_Now,Cube_Now_Type,Position_Projection_X,Position_Projection_Y,Cube_Now_Y);
                    CUBE_PROJECTION('+', Cube_Now, Cube_Now_Type, Position_Projection_X, Position_Projection_Y);
                    CUBE('+', Cube_Now, Cube_Now_Type, Cube_Now_X, Cube_Now_Y);
                    Add_Cube = false;
                    break;
                case "UpArrow":// 判斷可不可以旋轉
                    if(End){
                        break;
                    }
                    if(Cube_Now_Type+1==5){
                        if( !HIT("Spin", 1) ){
                            break;
                        }
                    }
                    else if( !HIT("Spin", Cube_Now_Type+1) ){
                        break;
                    }                    
                    CUBE('-', Cube_Now, Cube_Now_Type, Cube_Now_X, Cube_Now_Y);
                    CUBE_PROJECTION('-', Cube_Now, Cube_Now_Type, Position_Projection_X, Position_Projection_Y);
                    if(++Cube_Now_Type==5){
                        Cube_Now_Type = 1;
                    }
                    Position_Projection_Y = JUDGE_Projection_XY(Cube_Now,Cube_Now_Type,Position_Projection_X,Position_Projection_Y,Cube_Now_Y);
                    CUBE_PROJECTION('+', Cube_Now, Cube_Now_Type, Position_Projection_X, Position_Projection_Y);
                    CUBE('+', Cube_Now, Cube_Now_Type, Cube_Now_X, Cube_Now_Y);
                    CUBE('+', Cube_Now, Cube_Now_Type, Cube_Now_X, Cube_Now_Y);
                    Add_Cube = false;
                    break;
                case "DownArrow":// 強制往下-->判斷是否位置跟投影重複、消除投影、得分-->升等
                    if(End){
                        break;
                    }
                    CUBE('-', Cube_Now, Cube_Now_Type, Cube_Now_X, Cube_Now_Y);
                    Cube_Now_Y += 1;
                    if(Cube_Now_Y == Position_Projection_Y || Cube_Now_Y>=20){
                        Cube_Now_Y = Position_Projection_Y;
                        CUBE('+', Cube_Now, Cube_Now_Type, Cube_Now_X, Cube_Now_Y);
                        CUBE_DISAPPEAR();
                        Add_Cube = true;
                    }
                    else{
                        CUBE('+', Cube_Now, Cube_Now_Type, Cube_Now_X, Cube_Now_Y);
                        Add_Cube = false;
                    }
                    break;
                case "Spacebar":// 直接到底-->直接將作標設成投影座標、消除投影、得分-->升等
                    if(End){
                        break;
                    }
                    CUBE('-', Cube_Now, Cube_Now_Type, Cube_Now_X, Cube_Now_Y);
                    Cube_Now_Y = Position_Projection_Y;
                    CUBE('+', Cube_Now, Cube_Now_Type, Cube_Now_X, Cube_Now_Y);
                    CUBE_DISAPPEAR();
                    Add_Cube = true;
                    break;
                case "C":// 交換方塊-->重新投影、回到初始掉落位置，不可重複交換
                    CUBE('-', Cube_Now, Cube_Now_Type, Cube_Now_X, Cube_Now_Y);
                    CUBE_PROJECTION('-', Cube_Now, Cube_Now_Type, Position_Projection_X, Position_Projection_Y);
                    if(Cube_Hold == ' '){ // 第一次交換
                        Cube_Hold = Cube_Now;
                        Cube_Now = Cube_Next;
                        Cube_Next = RANDOM_CUBE();
                        CLEAR_NEXT();
                        CUBE('+', Cube_Next, 1, Cube_Next_X, Cube_Next_Y); // NEXT
                    }
                    else{
                        char Temp = Cube_Now;
                        Cube_Now = Cube_Hold;
                        Cube_Hold = Temp;
                    }
                    CLEAR_HOAD();
                    CUBE('+', Cube_Hold, 1, Cube_Hold_X, Cube_Hold_Y); // HOLD
                    Cube_Now_X = 28; Cube_Now_Y = 2; Position_Projection_X = 28;
                    Position_Projection_Y = JUDGE_Projection_XY(Cube_Now,Cube_Now_Type,Position_Projection_X,Position_Projection_Y,Cube_Now_Y);
                    CUBE_PROJECTION('+', Cube_Now, Cube_Now_Type, Position_Projection_X, Position_Projection_Y);
                    CUBE('+', Cube_Now, Cube_Now_Type, Cube_Now_X, Cube_Now_Y); //PLAY
                    Add_Cube = false;
                    break;
                default:
                    Add_Cube = false;
                    break;
            }
        }
        // 判斷向左、向右或旋轉後是否撞到邊界或方塊
        static bool HIT(string LRS, int Type){
            // Spin Type == 旋轉後的圖案
            switch(Cube_Now){
                case 'I':
                    switch(Type){
                        case 1: // I1
                        case 3:
                            switch(LRS){
                                case "Left":
                                    if( Tetris_Array[Cube_Now_Y, Cube_Now_X-4]=='a'){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Right":
                                    if( Tetris_Array[Cube_Now_Y, Cube_Now_X+6]=='a'){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Spin":
                                    if( (Tetris_Array[Cube_Now_Y, Cube_Now_X-2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+4]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                default:
                                    return false;
                            }
                        case 2: //I2
                        case 4:
                            switch(LRS){
                                case "Left":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X-2]=='a') &
                                        (Tetris_Array[Cube_Now_Y+2, Cube_Now_X-2]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Right":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X+2]=='a') &
                                        (Tetris_Array[Cube_Now_Y+2, Cube_Now_X+2]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Spin":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X]=='a') &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X]=='a') &
                                        (Tetris_Array[Cube_Now_Y+2, Cube_Now_X]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                default:
                                    return false;
                            }
                        default:
                            return false;
                    }
                case 'O':
                    switch(LRS){
                        case "Left":
                            if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-4]=='a') &
                                (Tetris_Array[Cube_Now_Y, Cube_Now_X-4]=='a')   ){
                                return true;
                            }
                            else{
                                return false;
                            }
                        case "Right":
                            if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+2]=='a') &
                                (Tetris_Array[Cube_Now_Y, Cube_Now_X+2]=='a')   ){
                                return true;
                            }
                            else{
                                return false;
                            }
                        default:
                            return false;
                    }
                case 'Z':
                    switch(Type){
                        case 1: // Z1
                        case 3:
                            switch(LRS){
                                case "Left":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-4]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-2]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Right":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+4]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Spin":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-2]=='a') &
                                        (Tetris_Array[Cube_Now_Y-1, Cube_Now_X]=='a')   &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+2]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                default:
                                    return false;
                            }
                        case 2: // Z2
                        case 4:
                            switch(LRS){
                                case "Left":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-4]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X-4]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Right":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Spin":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X]=='a')   &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X-2]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                default:
                                    return false;
                            }
                        default:
                            return false;
                    }
                case 'S':
                    switch(Type){
                        case 1: // S1
                        case 3:
                            switch(LRS){
                                case "Left":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-4]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Right":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+4]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+2]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Spin":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X]=='a')   &
                                        (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-2]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                default:
                                    return false;
                            }
                        case 2: // S2
                        case 4:
                            switch(LRS){
                                case "Left":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Right":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+4]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X+4]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Spin":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X]=='a')   &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X+2]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                default:
                                    return false;
                            }
                        default:
                            return false;
                    }
                case 'J':
                    switch(Type){
                        case 1: // J1
                            switch(LRS){
                                case "Left":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-4]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-4]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Right":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+4]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Spin":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+2]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                default:
                                    return false;
                            }
                        case 2: // J2
                            switch(LRS){
                                case "Left":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X-2]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Right":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+4]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X+2]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Spin":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X]=='a')   &
                                        (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+2]=='a') &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                default:
                                    return false;
                            }
                        case 3: // J3
                            switch(LRS){
                                case "Left":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-4]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X]=='a')     ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Right":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+4]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+4]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Spin":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-2]=='a') &
                                        (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+2]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                default:
                                    return false;
                            }
                        case 4: // J4
                            switch(LRS){
                                case "Left":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X-4]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Right":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X+2]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Spin":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X-2]=='a') &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                default:
                                    return false;
                            }                            
                        default:
                            return false;
                    }                
                case 'L':
                    switch(Type){
                        case 1: // L1
                            switch(LRS){
                                case "Left":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-4]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Right":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+4]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+4]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Spin":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+2]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                default:
                                    return false;
                            }
                        case 2: // L2
                            switch(LRS){
                                case "Left":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X-2]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Right":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X+4]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Spin":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X]=='a') &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X+2]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                default:
                                    return false;
                            }
                        case 3: // L3
                            switch(LRS){
                                case "Left":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-4]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-4]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Right":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+4]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X]=='a')     ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Spin":
                                    if( (Tetris_Array[Cube_Now_Y, Cube_Now_X-2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X-2]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                default:
                                    return false;
                            }
                        case 4: // L4
                            switch(LRS){
                                case "Left":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-4]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X-2]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Right":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X+2]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Spin":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-2]=='a') &
                                        (Tetris_Array[Cube_Now_Y-1, Cube_Now_X]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                default:
                                    return false;
                            }                            
                        default:
                            return false;
                    }
                case 'T':
                    switch(Type){
                        case 1: // T1
                            switch(LRS){
                                case "Left":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-4]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Right":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+4]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Spin":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+2]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                default:
                                    return false;
                            }
                        case 2: // T2
                            switch(LRS){
                                case "Left":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X-2]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Right":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+4]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X+2]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Spin":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X+2]=='a') &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                default:
                                    return false;
                            }
                        case 3: // T3
                            switch(LRS){
                                case "Left":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-4]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-2]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Right":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+4]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+2]=='a')   ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Spin":
                                    if( (Tetris_Array[Cube_Now_Y, Cube_Now_X-2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                default:
                                    return false;
                            }
                        case 4: // T4
                            switch(LRS){
                                case "Left":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X-2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-4]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X-2]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Right":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X+2]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X+2]=='a')   &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X+2]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                case "Spin":
                                    if( (Tetris_Array[Cube_Now_Y-1, Cube_Now_X]=='a') &
                                        (Tetris_Array[Cube_Now_Y, Cube_Now_X-2]=='a') &
                                        (Tetris_Array[Cube_Now_Y+1, Cube_Now_X]=='a') ){
                                        return true;
                                    }
                                    else{
                                        return false;
                                    }
                                default:
                                    return false;
                            }                            
                        default:
                            return false;
                    }
                default:
                    return false;
            }
        }
        // TETRIS的文字
        static void WORD_TETRIS(int Y){
            //TETRIS
            Console.BackgroundColor = ConsoleColor.Blue;
            GOTOXY(21, Y);Console.Write("   ");
            GOTOXY(27, Y);Console.Write("   ");
            GOTOXY(33, Y);Console.Write("   ");
            for(int i=0; i<4; i++){
                GOTOXY(22, (Y+1)+i);Console.Write(" ");
                GOTOXY(28, (Y+1)+i);Console.Write(" ");
                GOTOXY(34, (Y+1)+i);Console.Write(" ");
            }
            GOTOXY(33, Y+4);Console.Write(" ");
            GOTOXY(35, Y+4);Console.Write(" ");
            Console.BackgroundColor = ConsoleColor.White;
            GOTOXY(24, Y);Console.Write("   ");
            GOTOXY(30, Y);Console.Write("  ");
            GOTOXY(37, Y);Console.Write("  ");
            for(int i=0; i<4; i++){
                GOTOXY(24, (Y+1)+i);Console.Write(" ");
                GOTOXY(30, (Y+1)+i);Console.Write(" ");
            }
            GOTOXY(25, Y+2);Console.Write("  ");
            GOTOXY(25, Y+4);Console.Write("  ");
            GOTOXY(37, Y);Console.Write("  ");
            GOTOXY(32, Y+1);Console.Write(" ");
            GOTOXY(32, Y+2);Console.Write(" ");
            GOTOXY(31, Y+3);Console.Write(" ");
            GOTOXY(32, Y+4);Console.Write(" ");
            GOTOXY(36, Y+1);Console.Write(" ");
            GOTOXY(37, Y+2);Console.Write(" ");
            GOTOXY(38, Y+3);Console.Write(" ");
            GOTOXY(36, Y+4);Console.Write("  ");
        }
        // PLAY的文字
        static void WORD_PLAY(){
            Console.BackgroundColor = ConsoleColor.Blue;
            GOTOXY(24, 9);Console.Write("   ");
            GOTOXY(31, 9);Console.Write(" ");
            for(int i=0; i<4; i++){
                GOTOXY(24, 10+i);Console.Write(" ");
                GOTOXY(30, 10+i);Console.Write(" ");
                GOTOXY(32, 10+i);Console.Write(" ");
            }
            GOTOXY(26, 10);Console.Write(" ");
            GOTOXY(25, 11);Console.Write("  ");
            GOTOXY(31, 11);Console.Write(" ");
            Console.BackgroundColor = ConsoleColor.White;
            for(int i=0; i<4; i++){
                GOTOXY(27, 9+i);Console.Write(" ");
            }
            GOTOXY(27, 13);Console.Write("   ");
            for(int i=0; i<2; i++){
                GOTOXY(33, 9+i);Console.Write(" ");
                GOTOXY(35, 9+i);Console.Write(" ");
                GOTOXY(34, 11+i);Console.Write(" ");
            }
            GOTOXY(34, 13);Console.Write(" ");
            GOTOXY(0, 0);
        }
        // END的文字
        static void WORD_END(){
            //E
            Console.BackgroundColor = ConsoleColor.Blue;
            for(int i=0; i<3; i++){
                GOTOXY(24, 15+2*i);Console.Write("   ");
            }
            GOTOXY(24, 16);Console.Write(" ");
            GOTOXY(24, 18);Console.Write(" ");
            //N
            Console.BackgroundColor = ConsoleColor.White;
            for(int i=0; i<5; i++){
                GOTOXY(27, 15+i);Console.Write(" ");
                GOTOXY(31, 15+i);Console.Write(" ");
            }
            GOTOXY(28, 16);Console.Write(" ");
            GOTOXY(29, 17);Console.Write(" ");
            GOTOXY(30, 18);Console.Write(" ");
            //D
            Console.BackgroundColor = ConsoleColor.Blue;
            GOTOXY(32, 15);Console.Write("   ");
            GOTOXY(32, 19);Console.Write("   ");
            for(int i=0; i<3; i++){
                GOTOXY(32, 16+i);Console.Write(" ");
                GOTOXY(35, 16+i);Console.Write(" ");
            }
            GOTOXY(0, 0);
        }
        // LV的文字
        static void WORD_LV(){
            for(int i=0; i<4; i++){
                Console.BackgroundColor = ConsoleColor.Blue;
                GOTOXY(24, 15+i);Console.Write(" ");
                Console.BackgroundColor = ConsoleColor.White;
                GOTOXY(27, 15+i);Console.Write(" ");
                GOTOXY(29, 15+i);Console.Write(" ");
            }
            Console.BackgroundColor = ConsoleColor.Blue;
            GOTOXY(24, 19);Console.Write("   ");
            GOTOXY(31, 16);Console.Write(" ");
            GOTOXY(31, 18);Console.Write(" ");
            Console.BackgroundColor = ConsoleColor.White;
            GOTOXY(28, 19);Console.Write(" ");
            GOTOXY(0, 0);
        }
        //NEW的文字
        static void WORD_NEW(){
            for(int i=0; i<4; i++){
                Console.BackgroundColor = ConsoleColor.Blue;
                GOTOXY(20, 2+i);Console.Write(" ");
                GOTOXY(23, 2+i);Console.Write(" ");
                GOTOXY(27, 2+i);Console.Write(" ");
                GOTOXY(31, 2+i);Console.Write(" ");
                Console.BackgroundColor = ConsoleColor.White;
                GOTOXY(24, 2+i);Console.Write(" ");
            }
            Console.BackgroundColor = ConsoleColor.Blue;
            for(int i=0; i<2; i++){
                GOTOXY(21, 3+i);Console.Write(" ");
                GOTOXY(22, 4+i);Console.Write(" ");
                GOTOXY(29, 4+i);Console.Write(" ");
            }
            GOTOXY(20, 6);Console.Write(" ");
            GOTOXY(23, 6);Console.Write(" ");
            GOTOXY(28, 6);Console.Write("   ");
            Console.BackgroundColor = ConsoleColor.White;
            GOTOXY(25, 2);Console.Write("  ");
            GOTOXY(25, 4);Console.Write(" ");
            GOTOXY(24, 6);Console.Write("   ");
            GOTOXY(0, 0);        
        }
        // SCORE的文字
        static void WORD_SCORE(){
            for(int i=0; i<3; i++){
                Console.BackgroundColor = ConsoleColor.Blue;
                GOTOXY(24, 9+i);Console.Write(" ");
                GOTOXY(30, 9+i);Console.Write(" ");
                Console.BackgroundColor = ConsoleColor.White;
                GOTOXY(27, 9+i);Console.Write(" ");
                GOTOXY(29, 9+i);Console.Write(" ");
                GOTOXY(33, 9+i);Console.Write(" ");
            }
            Console.BackgroundColor = ConsoleColor.White;
            GOTOXY(22, 8);Console.Write("  ");
            GOTOXY(21, 9);Console.Write(" ");
            GOTOXY(22, 10);Console.Write(" ");
            GOTOXY(23, 11);Console.Write(" ");
            GOTOXY(21, 12);Console.Write("  ");
            GOTOXY(28, 8);Console.Write(" ");
            GOTOXY(28, 12);Console.Write(" ");
            GOTOXY(33, 8);Console.Write("   ");
            GOTOXY(34, 10);Console.Write(" ");
            GOTOXY(33, 12);Console.Write("   ");
            Console.BackgroundColor = ConsoleColor.Blue;
            GOTOXY(25, 8);Console.Write("  ");
            GOTOXY(25, 12);Console.Write("  ");
            GOTOXY(30, 8);Console.Write("  ");
            GOTOXY(32, 9);Console.Write(" ");
            GOTOXY(32, 10);Console.Write(" ");
            GOTOXY(31, 11);Console.Write(" ");
            GOTOXY(30, 12);Console.Write(" ");
            GOTOXY(32, 12);Console.Write(" ");
            GOTOXY(0, 0);
        }
        // SELECT的文字
        static void WORD_SELECT(char AS, int X, int Y){
            if(AS=='+'){
                Console.BackgroundColor = ConsoleColor.White;
            }
            else{
                Console.BackgroundColor = ConsoleColor.Black;
            }
            GOTOXY(X, Y);Console.Write(" ");
            GOTOXY(X+1, Y+1);Console.Write(" ");
            GOTOXY(X+2, Y+2);Console.Write(" ");
            GOTOXY(X+1, Y+3);Console.Write(" ");
            GOTOXY(X, Y+4);Console.Write(" ");
            GOTOXY(0, 0);
            Console.BackgroundColor = ConsoleColor.Black;
        }
        // 印出成績
        static void RANK_SCORE(){ // 成績
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            DATABASE_HIGHSCORE();
            GOTOXY(7, 13);Console.Write(Score);// Score
            GOTOXY(7, 16);Console.Write(Level);// Level
            GOTOXY(7, 19);Console.Write(Line_Count);// Lines
            for(int i=0; i<4; i++){
                GOTOXY(46, 13+2*i);Console.Write(HighScoreName[i]);
                GOTOXY(54, 13+2*i);Console.Write(HighScore[i]);
            }
            GOTOXY(0, 0);
        }
        // 清除遊戲框格
        static void CLEAR_PLAY(){ 
            Console.BackgroundColor = ConsoleColor.Black;
            for(int i=0; i<20; i++){
                GOTOXY(20, 1+i);Console.Write("                    ");
            }
            GOTOXY(0, 0);
        }
        // 清除HOLD框格
        static void CLEAR_HOAD(){
            Console.BackgroundColor = ConsoleColor.Black;
            for(int i=0; i<4; i++){
                GOTOXY(2, 4+i);Console.Write("            ");
            }
            GOTOXY(0, 0);
        }
        // 清除NEXT框格
        static void CLEAR_NEXT(){
            Console.BackgroundColor = ConsoleColor.Black;
            for(int i=0; i<4; i++){
                GOTOXY(46, 4+i);Console.Write("            ");
            }
            GOTOXY(0, 0);
        }
        // 印出數字
        static void NUMBER(char AS, int N, int X, int Y){ // 列印0123456789
            if(AS=='+'){
                Console.BackgroundColor = ConsoleColor.White;
            }
            else{
                Console.BackgroundColor = ConsoleColor.Black;
                for(int i=0; i<5; i++){
                    GOTOXY(X, Y+i);
                    Console.Write("   ");
                }
                GOTOXY(0, 0);
                return;
            }
            switch(N){
                case 0:
                    for(int i=0; i<5; i++){
                        GOTOXY(X, Y+i);Console.Write(" ");
                        GOTOXY(X+2, Y+i);Console.Write(" ");
                    }
                    GOTOXY(X+1, Y);Console.Write(" ");
                    GOTOXY(X+1, Y+4);Console.Write(" ");
                    break;
                case 1:
                    for(int i=1; i<=3; i++){
                        GOTOXY(X+1, Y+i);Console.Write(" ");
                    }
                    GOTOXY(X, Y);Console.Write("  ");
                    GOTOXY(X, Y+4);Console.Write("   ");
                    break;
                case 2:
                    for(int i=0; i<3; i++){
                        GOTOXY(X, Y+2*i);Console.Write("   ");
                    }
                    GOTOXY(X+2, Y+1);Console.Write(" ");
                    GOTOXY(X, Y+3);Console.Write(" ");
                    break;
                case 3:
                    for(int i=0; i<3; i++){
                        GOTOXY(X, Y+2*i);Console.Write("   ");
                    }
                    GOTOXY(X+2, Y+1);Console.Write(" ");
                    GOTOXY(X+2, Y+3);Console.Write(" ");
                    break;
                case 4:
                    for(int i=0; i<2; i++){
                        GOTOXY(X, Y+i);Console.Write(" ");
                        GOTOXY(X+2, Y+i);Console.Write(" ");
                        GOTOXY(X+2, Y+3+i);Console.Write(" ");
                    }
                    GOTOXY(X, Y+2);Console.Write("   ");
                    break;
                case 5:
                    for(int i=0; i<3; i++){
                        GOTOXY(X, Y+2*i);Console.Write("   ");
                    }
                    GOTOXY(X, Y+1);Console.Write(" ");
                    GOTOXY(X+2, Y+3);Console.Write(" ");
                    break;
                case 6:
                    for(int i=0; i<3; i++){
                        GOTOXY(X, Y+2*i);Console.Write("   ");
                    }
                    GOTOXY(X, Y+1);Console.Write(" ");
                    GOTOXY(X, Y+3);Console.Write(" ");
                    GOTOXY(X+2, Y+3);Console.Write(" ");
                    break;
                case 7:
                    for(int i=1; i<=4; i++){
                        GOTOXY(X+2, Y+i);Console.Write(" ");
                    }
                    GOTOXY(X, Y);Console.Write("   ");
                    break;
                case 8:
                    for(int i=0; i<3; i++){
                        GOTOXY(X, Y+2*i);Console.Write("   ");
                    }
                    for(int i=1; i<=3; i++){
                        GOTOXY(X, Y+i);Console.Write(" ");
                        GOTOXY(X+2, Y+i);Console.Write(" ");
                    }
                    break;
                case 9:
                    for(int i=0; i<3; i++){
                        GOTOXY(X, Y+2*i);Console.Write("   ");
                    }
                    GOTOXY(X, Y+1);Console.Write(" ");
                    GOTOXY(X+2, Y+1);Console.Write(" ");
                    GOTOXY(X+2, Y+3);Console.Write(" ");
                    break;
                default:
                    break;              
            }
            GOTOXY(0, 0);
        }
        // 遊戲開始的前置作業
        static void START(){
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();
            GAME_FRAME();
            GOTOXY(6, 2);Console.Write("Hold");
            GOTOXY(50, 2);Console.Write("Next");
            GOTOXY(5, 11);Console.Write("Score");
            GOTOXY(5, 15);Console.Write("Level");
            GOTOXY(5, 18);Console.Write("Lines");
            GOTOXY(47, 11);Console.Write("High Score");
            WORD_TETRIS(2);
            WORD_PLAY();
            WORD_LV();
            NUMBER('+', 0, 33, 15);
            NUMBER('+', 1, 37, 15);
            RANK_SCORE(); // 連接資料庫讀取排名資料
            SELECT_PLAY("LEVEL");
            CLEAR_PLAY();
            GOTOXY(0, 0);
        }
        // 遊戲結束時的動作
        static void GAMEOVER(){
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            CLEAR_PLAY();
            CLEAR_HOAD();
            CLEAR_NEXT();
            WORD_NEW();
            WORD_SCORE();
            NUMBER('+', Score/10000, 21, 15);
            Score %= 10000;
            NUMBER('+', Score/1000, 25, 15);
            Score %= 1000;
            NUMBER('+', Score/100, 29, 15);
            Score %= 100;
            NUMBER('+', Score/10, 33, 15);
            Score %= 10;
            NUMBER('+', Score/1, 37, 15);
            Score = 0;
            RANK_SCORE();
            GOTOXY(0, 0);
            Thread.Sleep(3000);
            CLEAR_PLAY();
            WORD_TETRIS(2);
            WORD_PLAY();
            WORD_END();
            SELECT_PLAY("END");
        }
        // 在座標上新增或刪除方塊
        static void CUBE(char AS, char Cube, int N, int X, int Y){
            switch(Cube){
                case 'I':
                    if(AS=='+'){
                        Console.ForegroundColor = ConsoleColor.Cyan;
                    }
                    else if(AS=='-'){
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    switch(N){
                        case 1:
                        case 3:
                            GOTOXY(X-2, Y);Console.Write("■■■■");
                            GOTOXY(0, 0);
                            break;
                        case 2:
                        case 4:
                            GOTOXY(X, Y-1);Console.Write("■");
                            GOTOXY(X, Y);Console.Write("■");
                            GOTOXY(X, Y+1);Console.Write("■");
                            GOTOXY(X, Y+2);Console.Write("■");
                            GOTOXY(0, 0);
                            break;
                    }
                break;
                case 'J':
                    if(AS=='+'){
                        Console.ForegroundColor = ConsoleColor.DarkBlue;
                    }
                    else if(AS=='-'){
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    switch(N){
                        case 1:
                            GOTOXY(X-2, Y-1);Console.Write("■");
                            GOTOXY(X-2, Y);Console.Write("■■■");
                            GOTOXY(0, 0);
                            break;
                        case 2:
                            GOTOXY(X, Y-1);Console.Write("■■");
                            GOTOXY(X, Y);Console.Write("■");
                            GOTOXY(X, Y+1);Console.Write("■");
                            GOTOXY(0, 0);
                            break;
                        case 3:
                            GOTOXY(X-2, Y);Console.Write("■■■");
                            GOTOXY(X+2, Y+1);Console.Write("■");
                            GOTOXY(0, 0);
                            break;
                        case 4:
                            GOTOXY(X-2, Y+1);Console.Write("■■");
                            GOTOXY(X, Y);Console.Write("■");
                            GOTOXY(X, Y-1);Console.Write("■");
                            GOTOXY(0, 0);
                            break;
                    }
                    break;
                case 'L':
                    if(AS=='+'){
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                    }
                    else if(AS=='-'){
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    switch(N){
                        case 1:
                            GOTOXY(X+2, Y-1);Console.Write("■");
                            GOTOXY(X-2, Y);Console.Write("■■■");
                            GOTOXY(0, 0);
                            break;
                        case 2:
                            GOTOXY(X, Y+1);Console.Write("■■");
                            GOTOXY(X, Y);Console.Write("■");
                            GOTOXY(X, Y-1);Console.Write("■");
                            GOTOXY(0, 0);
                            break;
                        case 3:
                            GOTOXY(X-2, Y);Console.Write("■■■");
                            GOTOXY(X-2, Y+1);Console.Write("■");
                            GOTOXY(0, 0);
                            break;
                        case 4:
                            GOTOXY(X-2, Y-1);Console.Write("■■");
                            GOTOXY(X, Y);Console.Write("■");
                            GOTOXY(X, Y+1);Console.Write("■");
                            GOTOXY(0, 0);
                            break;
                    }
                    break;
                case 'O':
                    if(AS=='+'){
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    else if(AS=='-'){
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    GOTOXY(X-2, Y-1);Console.Write("■■");
                    GOTOXY(X-2, Y);Console.Write("■■");
                    GOTOXY(0, 0);
                    break;
                case 'S':
                    if(AS=='+'){
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else if(AS=='-'){
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    switch(N){
                        case 1:
                        case 3:
                            GOTOXY(X, Y-1);Console.Write("■■");
                            GOTOXY(X-2, Y);Console.Write("■■");
                            GOTOXY(0, 0);
                            break;
                        case 2:
                        case 4:
                            GOTOXY(X, Y);Console.Write("■■");
                            GOTOXY(X, Y-1);Console.Write("■");
                            GOTOXY(X+2, Y+1);Console.Write("■");
                            GOTOXY(0, 0);
                            break;
                    }
                    break;
                case 'T':
                    if(AS=='+'){
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    }
                    else if(AS=='-'){
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    switch(N){
                        case 1:
                            GOTOXY(X, Y-1);Console.Write("■");
                            GOTOXY(X-2, Y);Console.Write("■■■");
                            GOTOXY(0, 0);
                            break;
                        case 2:
                            GOTOXY(X, Y);Console.Write("■■");
                            GOTOXY(X, Y-1);Console.Write("■");
                            GOTOXY(X, Y+1);Console.Write("■");
                            GOTOXY(0, 0);
                            break;
                        case 3:
                            GOTOXY(X-2, Y);Console.Write("■■■");
                            GOTOXY(X, Y+1);Console.Write("■");
                            GOTOXY(0, 0);
                            break;
                        case 4:
                            GOTOXY(X-2, Y);Console.Write("■■");
                            GOTOXY(X, Y-1);Console.Write("■");
                            GOTOXY(X, Y+1);Console.Write("■");
                            GOTOXY(0, 0);
                            break;
                    }
                    break;
                case 'Z':
                    if(AS=='+'){
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    else if(AS=='-'){
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    switch(N){
                        case 1:
                        case 3:
                            GOTOXY(X-2, Y-1);Console.Write("■■");
                            GOTOXY(X, Y);Console.Write("■■");
                            GOTOXY(0, 0);
                            break;
                        case 2:
                        case 4:
                            GOTOXY(X-2, Y);Console.Write("■■");
                            GOTOXY(X, Y-1);Console.Write("■");
                            GOTOXY(X-2, Y+1);Console.Write("■");
                            GOTOXY(0, 0);
                            break;
                    }
                    break;
            }
        }
        // 隨機選擇方塊的型態
        static char RANDOM_CUBE(){
            Random rnd = new Random();
            int N = rnd.Next(7);//0~6
            return Cube[N];
        }
        // 遊戲開始的框格
        static void GAME_FRAME(){
            GOTOXY(0,  0);Console.Write("                  ┌─────────────────────┐                  ");
            GOTOXY(0,  1);Console.Write("┌─────────────┐   │                     │   ┌─────────────┐");
            GOTOXY(0,  2);Console.Write("│             │   │                     │   │             │");
            GOTOXY(0,  3);Console.Write("│─────────────│   │                     │   │─────────────│");
            GOTOXY(0,  4);Console.Write("│             │   │                     │   │             │");
            GOTOXY(0,  5);Console.Write("│             │   │                     │   │             │");
            GOTOXY(0,  6);Console.Write("│             │   │                     │   │             │");
            GOTOXY(0,  7);Console.Write("│             │   │                     │   │             │");
            GOTOXY(0,  8);Console.Write("└─────────────┘   │                     │   └─────────────┘");
            GOTOXY(0,  9);Console.Write("                  │                     │                  ");
            GOTOXY(0, 10);Console.Write("┌─────────────┐   │                     │   ┌─────────────┐");
            GOTOXY(0, 11);Console.Write("│             │   │                     │   │             │");
            GOTOXY(0, 12);Console.Write("│─────────────│   │                     │   │─────────────│");
            GOTOXY(0, 13);Console.Write("│             │   │                     │   │             │");
            GOTOXY(0, 14);Console.Write("│─────────────│   │                     │   │─────────────│");
            GOTOXY(0, 15);Console.Write("│             │   │                     │   │             │");
            GOTOXY(0, 16);Console.Write("│             │   │                     │   │─────────────│");
            GOTOXY(0, 17);Console.Write("│─────────────│   │                     │   │             │");
            GOTOXY(0, 18);Console.Write("│             │   │                     │   │─────────────│");
            GOTOXY(0, 19);Console.Write("│             │   │                     │   │             │");
            GOTOXY(0, 20);Console.Write("└─────────────┘   │                     │   └─────────────┘");
            GOTOXY(0, 21);Console.Write("                  └─────────────────────┘                  ");
        }
        // 判斷方塊的投影位置
        static int JUDGE_Projection_XY(char Cube, int N, int X, int Y, int Cube_Y){
            int Temp_High1 = 21;
            int Temp_High2 = 21;
            int Temp_High3 = 21;
            int Temp_High4 = 21;
            switch(Cube){
                case 'I':
                    switch(N){
                        case 1:
                        case 3:
                            for(int i=Cube_Y; i<21; i++){
                                if(Tetris_Array[i, X-2] != 'a'){
                                    Temp_High1 = i;
                                    break;
                                }
                            }
                            for(int j=Cube_Y; j<21; j++){
                                if(Tetris_Array[j, X] != 'a'){
                                    Temp_High2 = j;
                                    break;
                                }
                            }
                            for(int k=Cube_Y; k<21; k++){
                                if(Tetris_Array[k, X+2] != 'a'){
                                    Temp_High3 = k;
                                    break;
                                }
                            }
                            for(int l=Cube_Y; l<21; l++){
                                if(Tetris_Array[l, X+4] != 'a'){
                                    Temp_High4 = l;
                                    break;
                                }
                            }
                            if(Temp_High1<=Temp_High2 && Temp_High1<=Temp_High3 && Temp_High1<=Temp_High4){
                                Y = Temp_High1-1;
                            }
                            else if(Temp_High2<=Temp_High1 && Temp_High2<=Temp_High3 && Temp_High2<=Temp_High4){
                                Y = Temp_High2-1;
                            }
                            else if(Temp_High3<=Temp_High1 && Temp_High3<=Temp_High2 && Temp_High3<=Temp_High4){
                                Y = Temp_High3-1;
                            }
                            else if(Temp_High4<=Temp_High1 && Temp_High4<=Temp_High2 && Temp_High4<=Temp_High3){
                                Y = Temp_High4-1;
                            }
                            break;
                        case 2:
                        case 4:
                            for(int i=Cube_Y; i<21; i++){
                                if(Tetris_Array[i, X] != 'a'){
                                    Temp_High1 = i;
                                    break;
                                }
                            }
                            Y = Temp_High1-3;
                            break;
                    }
                break;
                case 'J':
                    switch(N){
                        case 1:
                            for(int i=Cube_Y; i<21; i++){
                                if(Tetris_Array[i,X-2] != 'a'){
                                    Temp_High1 = i;
                                    break;
                                }
                            }
                            for(int j=Cube_Y; j<21; j++){
                                if(Tetris_Array[j, X] != 'a'){
                                    Temp_High2 = j;
                                    break;
                                }
                            }
                            for(int k=Cube_Y; k<21; k++){
                                if(Tetris_Array[k, X+2] != 'a'){
                                    Temp_High3 = k;
                                    break;
                                }
                            }
                            if(Temp_High1 == Temp_High2 && Temp_High2 == Temp_High3){
                                Y = Temp_High1-1;
                            }
                            else if(Temp_High1<=Temp_High2 && Temp_High1<=Temp_High3){
                                Y = Temp_High1-1;
                            }
                            else if(Temp_High2<=Temp_High1 && Temp_High2<=Temp_High3){
                                Y = Temp_High2-1;
                            }
                            else if(Temp_High3<=Temp_High1 && Temp_High3<=Temp_High2){
                                Y = Temp_High3-1;
                            }
                            break;
                        case 2:
                            for(int i=Cube_Y; i<21; i++){
                                if(Tetris_Array[i, X] != 'a'){
                                    Temp_High1 = i;
                                    break;
                                }
                            }
                            for(int j=Cube_Y; j<21; j++){
                                if(Tetris_Array[j, X+2] != 'a'){
                                    Temp_High2 = j;
                                    break;
                                }
                            }
                            if(Temp_High2<Temp_High1 && (Temp_High1-Temp_High2)>=2){
                                Y = Temp_High2;
                            }
                            else{
                                Y = Temp_High1-2;
                            }
                            break;
                        case 3:
                            for(int i=Cube_Y; i<21; i++){
                                if(Tetris_Array[i, X-2] != 'a'){
                                    Temp_High1 = i;
                                    break;
                                }
                            }
                            for(int j=Cube_Y; j<21; j++){
                                if(Tetris_Array[j,X] != 'a'){
                                    Temp_High2 = j;
                                    break;
                                }
                            }
                            for(int k=Cube_Y; k<21; k++){
                                if(Tetris_Array[k, X+2] != 'a'){
                                    Temp_High3 = k;
                                    break;
                                }
                            }
                            if(Temp_High1 == Temp_High2 && Temp_High2 == Temp_High3){
                                Y =Temp_High3-2;
                            }
                            else if(Temp_High3<=Temp_High2 && Temp_High3<=Temp_High1){
                                Y = Temp_High3-2;
                            }
                            else if(Temp_High1<=Temp_High2 && Temp_High1<=Temp_High3){
                                Y = Temp_High1-1;
                            }
                            else if(Temp_High2<=Temp_High1 && Temp_High2<=Temp_High3){
                                Y = Temp_High2-1;
                            }
                            break;
                        case 4:
                            for(int i=Cube_Y+1; i<21; i++){
                                if(Tetris_Array[i, X-2] != 'a'){
                                    Temp_High1 = i;
                                    break;
                                }
                            }
                            for(int j=Cube_Y+1; j<21; j++){
                                if(Tetris_Array[j, X] != 'a'){
                                    Temp_High2 = j;
                                    break;
                                }
                            }
                            if(Temp_High1 <= Temp_High2){
                                Y = Temp_High1-2;
                            }
                            else if(Temp_High2 <= Temp_High1){
                                Y = Temp_High2-2;
                            }
                            break;
                    }
                    break;
                case 'L':
                    switch(N){
                        case 1:
                            for(int i=Cube_Y; i<21; i++){
                                if(Tetris_Array[i, X-2] != 'a'){
                                    Temp_High1 = i;
                                    break;
                                }
                            }
                            for(int j=Cube_Y; j<21; j++){
                                if(Tetris_Array[j, X] != 'a'){
                                    Temp_High2 = j;
                                    break;
                                }
                            }
                            for(int k=Cube_Y; k<21; k++){
                                if(Tetris_Array[k, X+2] != 'a'){
                                    Temp_High3 = k;
                                    break;
                                }
                            }
                            if(Temp_High1 == Temp_High2 && Temp_High2 == Temp_High3){
                                Y = Temp_High1-1;
                            }
                            else if(Temp_High1<=Temp_High2 && Temp_High1<=Temp_High3){
                                Y = Temp_High1-1;
                            }
                            else if(Temp_High2<=Temp_High1 && Temp_High2<=Temp_High3){
                                Y = Temp_High2-1;
                            }
                            else if(Temp_High3<=Temp_High1 && Temp_High3<=Temp_High2){
                                Y = Temp_High3-1;
                            }
                            break;
                        case 2:
                            for(int i=Cube_Y+1; i<21; i++){
                                if(Tetris_Array[i, X] != 'a'){
                                    Temp_High1 = i;
                                    break;
                                }
                            }
                            for(int j=Cube_Y+1; j<21; j++){
                                if(Tetris_Array[j, X+2] != 'a'){
                                    Temp_High2 = j;
                                    break;
                                }
                            }
                            if(Temp_High1 <= Temp_High2){
                                Y = Temp_High1-2;
                            }
                            else if(Temp_High2 <= Temp_High1){
                                Y = Temp_High2-2;
                            }
                            break;
                        case 3:
                            for(int i=Cube_Y; i<21; i++){
                                if(Tetris_Array[i, X-2] != 'a'){
                                    Temp_High1 = i;
                                    break;
                                }
                            }
                            for(int j=Cube_Y; j<21; j++){
                                if(Tetris_Array[j, X] != 'a'){
                                    Temp_High2 = j;
                                    break;
                                }
                            }
                            for(int k=Cube_Y; k<21; k++){
                                if(Tetris_Array[k, X+2] != 'a'){
                                    Temp_High3 = k;
                                    break;
                                }
                            }
                            if(Temp_High1==Temp_High2 && Temp_High2==Temp_High3){
                                Y = Temp_High1-2;
                            }
                            else if(Temp_High1<=Temp_High2 && Temp_High1<=Temp_High3){
                                Y = Temp_High1-2;
                            }
                            else if(Temp_High2<=Temp_High1 && Temp_High2<=Temp_High3){
                                Y = Temp_High2-1;
                            }
                            else if(Temp_High3<=Temp_High1 && Temp_High3<=Temp_High2){
                                Y = Temp_High3-1;
                            }
                            break;
                        case 4:
                            for(int i=Cube_Y; i<21; i++){
                                if(Tetris_Array[i, X-2] != 'a'){
                                    Temp_High1 = i;
                                    break;
                                }
                            }
                            for(int j=Cube_Y; j<21; j++){
                                if(Tetris_Array[j, X] != 'a'){
                                    Temp_High2 = j;
                                    break;
                                }
                            }
                            if(Temp_High1<Temp_High2 && (Temp_High2-Temp_High1)>=2){
                                Y = Temp_High1;
                            }
                            else{
                                Y = Temp_High2-2;
                            }
                            break;
                    }
                    break;
                case 'O':
                    for(int i=Cube_Y; i<21; i++){
                        if(Tetris_Array[i, X-2] != 'a'){
                            Temp_High1 = i;
                            break;
                        }
                    }
                    for(int j=Cube_Y; j<21; j++){
                        if(Tetris_Array[j, X] != 'a'){
                            Temp_High2 = j;
                            break;
                        }
                    }
                    if(Temp_High1<=Temp_High2){
                        Y = Temp_High1-1;
                    }
                    else if(Temp_High2<=Temp_High1){
                        Y = Temp_High2-1;
                    }
                    break;
                case 'S':
                    switch(N){
                        case 1:
                        case 3:
                            for(int i=Cube_Y; i<21; i++){
                                if(Tetris_Array[i, X-2] != 'a'){
                                    Temp_High1 = i;
                                    break;
                                }
                            }
                            for(int j=Cube_Y; j<21; j++){
                                if(Tetris_Array[j, X] != 'a'){
                                    Temp_High2 = j;
                                    break;
                                }
                            }
                            for(int k=Cube_Y; k<21; k++){
                                if(Tetris_Array[k, X+2] != 'a'){
                                    Temp_High3 = k;
                                    break;
                                }
                            }
                            if(Temp_High3<Temp_High1 && Temp_High3<Temp_High2){
                                Y = Temp_High3;
                            }
                            else if(Temp_High1<=Temp_High2 && Temp_High1<=Temp_High3){
                                Y = Temp_High1-1;
                            }
                            else if(Temp_High2<=Temp_High1 && Temp_High2<=Temp_High3){
                                Y = Temp_High2-1;
                            }
                            break;
                        case 2:
                        case 4:
                            for(int i=Cube_Y; i<21; i++){
                                if(Tetris_Array[i, X] != 'a'){
                                    Temp_High1 = i;
                                    break;
                                }
                            }
                            for(int j=Cube_Y; j<21; j++){
                                if(Tetris_Array[j, X+2] != 'a'){
                                    Temp_High2 = j;
                                    break;
                                }
                            }
                            if(Temp_High1 < Temp_High2){
                                Y = Temp_High1-1;
                            }
                            else if(Temp_High2 <= Temp_High1){
                                Y = Temp_High2-2;
                            }
                            break;
                    }
                    break;
                case 'T':
                    switch(N){
                        case 1:
                            for(int i=Cube_Y; i<21; i++){
                                if(Tetris_Array[i, X-2] != 'a'){
                                    Temp_High1 = i;
                                    break;
                                }
                            }
                            for(int j=Cube_Y; j<21; j++){
                                if(Tetris_Array[j, X] != 'a'){
                                    Temp_High2 = j;
                                    break;
                                }
                            }
                            for(int k=Cube_Y; k<21; k++){
                                if(Tetris_Array[k, X+2] != 'a'){
                                    Temp_High3 = k;
                                    break;
                                }
                            }
                            if(Temp_High1 == Temp_High2 && Temp_High2 == Temp_High3){
                                Y = Temp_High1-1;
                            }
                            else if(Temp_High1<=Temp_High2 && Temp_High1<=Temp_High3){
                                Y = Temp_High1-1;
                            }
                            else if(Temp_High2<=Temp_High1 && Temp_High2<=Temp_High3){
                                Y = Temp_High2-1;
                            }
                            else if(Temp_High3<=Temp_High1 && Temp_High3<=Temp_High2){
                                Y = Temp_High3-1;
                            }
                            break;
                        case 2:
                            for(int i=Cube_Y; i<21; i++){
                                if(Tetris_Array[i, X] != 'a'){
                                    Temp_High1 = i;
                                    break;
                                }
                            }
                            for(int j=Cube_Y; j<21; j++){
                                if(Tetris_Array[j, X+2] != 'a'){
                                    Temp_High2 = j;
                                    break;
                                }
                            }
                            if(Temp_High2<Temp_High1){
                                Y = Temp_High2-1;
                            }
                            else if(Temp_High1<=Temp_High2){
                                Y = Temp_High1-2;
                            }
                            break;
                        case 3:
                            for(int i=Cube_Y; i<21; i++){
                                if(Tetris_Array[i, X-2] != 'a'){
                                    Temp_High1 = i;
                                    break;
                                }
                            }
                            for(int j=Cube_Y; j<21; j++){
                                if(Tetris_Array[j, X] != 'a'){
                                    Temp_High2 = j;
                                    break;
                                }
                            }
                            for(int k=Cube_Y; k<21; k++){
                                if(Tetris_Array[k, X+2] != 'a'){
                                    Temp_High3 = k;
                                    break;
                                }
                            }
                            if(Temp_High2==Temp_High1 && Temp_High1==Temp_High3){
                                Y = Temp_High2-2;
                            }
                            else if(Temp_High1<Temp_High2 && Temp_High1<=Temp_High3){
                                Y = Temp_High1-1;
                            }
                            else if(Temp_High3<=Temp_High1 && Temp_High3<Temp_High2){
                                Y = Temp_High3-1;
                            }
                            else if(Temp_High2<=Temp_High1 && Temp_High2<=Temp_High3){
                                Y = Temp_High2-2;
                            }
                            break;
                        case 4:
                            for(int i=Cube_Y; i<21; i++){
                                if(Tetris_Array[i, X-2] != 'a'){
                                    Temp_High1 = i;
                                    break;
                                }
                            }
                            for(int j=Cube_Y; j<21; j++){
                                if(Tetris_Array[j, X] != 'a'){
                                    Temp_High2 = j;
                                    break;
                                }
                            }
                            if(Temp_High1<Temp_High2){
                                Y = Temp_High1-1;
                            }
                            else if(Temp_High2<=Temp_High1){
                                Y = Temp_High2-2;
                            }
                            break;
                    }
                    break;
                case 'Z':
                    switch(N){
                        case 1:
                        case 3:
                            for(int i=Cube_Y; i<21; i++){
                                if(Tetris_Array[i, X-2] != 'a'){
                                    Temp_High1 = i;
                                    break;
                                }
                            }
                            for(int j=Cube_Y; j<21; j++){
                                if(Tetris_Array[j, X] != 'a'){
                                    Temp_High2 = j;
                                    break;
                                }
                            }
                            for(int k=Cube_Y; k<21; k++){
                                if(Tetris_Array[k, X+2] != 'a'){
                                    Temp_High3 = k;
                                    break;
                                }
                            }
                            if(Temp_High1<Temp_High2 && Temp_High1<Temp_High3){
                                Y = Temp_High1;
                            }
                            else if(Temp_High2<=Temp_High1 && Temp_High2<=Temp_High3){
                                Y = Temp_High2-1;
                            }
                            else if(Temp_High3<=Temp_High1 && Temp_High3<=Temp_High2){
                                Y = Temp_High3-1;
                            }
                            break;
                        case 2:
                        case 4:
                            for(int i=Cube_Y; i<21; i++){
                                if(Tetris_Array[i, X-2] != 'a'){
                                    Temp_High1 = i;
                                    break;
                                }
                            }
                            for(int j=Cube_Y; j<21; j++){
                                if(Tetris_Array[j, X] != 'a'){
                                    Temp_High2 = j;
                                    break;
                                }
                            }
                            if(Temp_High2 < Temp_High1){
                                Y = Temp_High2-1;
                            }
                            else if(Temp_High1 <= Temp_High2){
                                Y = Temp_High1-2;
                            }
                            break;
                    }
                    break;
            }
            return Y;
        }
        // 印出方塊的投影
        static void CUBE_PROJECTION(char AS, char Cube_Proiection, int N, int Projection_X, int Projection_Y){
            bool Array_Flag = true;
            int Array_X = 0, Array_Y = 0;
            switch(Cube_Proiection){
                case 'I':
                    if(AS=='+'){
                        Array_X = Projection_Y;
                        Array_Y = Projection_X;
                        Array_Flag = true;
                        Console.ForegroundColor = ConsoleColor.Cyan;
                    }
                    else if(AS=='-'){     
                        Array_X = Projection_Y;
                        Array_Y = Projection_X; 
                        Array_Flag = false;                
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    switch(N){
                        case 1:
                        case 3:
                            if(Array_Flag){
                                for(int i=0; i<8; i++){
                                    Tetris_Array[Array_X, Array_Y-2] = 'I';
                                    Array_Y++;
                                }
                            }
                            else{
                                for(int i=0; i<8; i++){
                                    Tetris_Array[Array_X, Array_Y-2] = 'a';
                                    Array_Y++;
                                }
                            }
                            GOTOXY(Projection_X-2, Projection_Y);
                            Console.Write("□□□□");
                            break;
                        case 2:
                        case 4:
                            if(Array_Flag){
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X-1, Array_Y] = 'I';
                                    Tetris_Array[Array_X-1, Array_Y+1] = 'I';
                                    Array_X++;
                                }
                            }
                            else{
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X-1, Array_Y] = 'a';
                                    Tetris_Array[Array_X-1, Array_Y+1] = 'a';
                                    Array_X++;
                                }
                            }
                            GOTOXY(Projection_X, Projection_Y-1);Console.Write("□");
                            GOTOXY(Projection_X, Projection_Y);Console.Write("□");
                            GOTOXY(Projection_X, Projection_Y+1);Console.Write("□");
                            GOTOXY(Projection_X, Projection_Y+2);Console.Write("□");
                            break;
                    }
                break;
                case 'J':
                    if(AS=='+'){
                        Array_X = Projection_Y;
                        Array_Y = Projection_X;
                        Array_Flag = true;
                        Console.ForegroundColor = ConsoleColor.DarkBlue;
                    }
                    else if(AS=='-'){
                        Array_X = Projection_Y;
                        Array_Y = Projection_X;
                        Array_Flag = false;
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    switch(N){
                        case 1:
                            if(Array_Flag){
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X-1, Array_Y-2] = 'J';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<6; i++){
                                    Tetris_Array[Array_X, Array_Y-2] = 'J';
                                    Array_Y++;
                                }
                            }
                            else{
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X-1, Array_Y-2] = 'a';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<6; i++){
                                    Tetris_Array[Array_X, Array_Y-2] = 'a';
                                    Array_Y++;
                                }
                            }
                            GOTOXY(Projection_X-2, Projection_Y-1);Console.Write("□");
                            GOTOXY(Projection_X-2, Projection_Y);Console.Write("□□□");
                            break;
                        case 2:
                            if(Array_Flag){
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X-1, Array_Y] = 'J';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X, Array_Y] = 'J';
                                    Tetris_Array[Array_X+1, Array_Y] = 'J';
                                    Array_Y++;
                                }
                            }
                            else{
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X-1, Array_Y] = 'a';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X, Array_Y] = 'a';
                                    Tetris_Array[Array_X+1, Array_Y] = 'a';
                                    Array_Y++;
                                }
                            }
                            GOTOXY(Projection_X, Projection_Y-1);Console.Write("□□");
                            GOTOXY(Projection_X, Projection_Y);Console.Write("□");
                            GOTOXY(Projection_X, Projection_Y+1);Console.Write("□");
                            break;
                        case 3:
                            if(Array_Flag){
                                for(int i=0; i<6; i++){
                                    Tetris_Array[Array_X, Array_Y-2] = 'J';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X+1, Array_Y+2] = 'J';
                                    Array_Y++;
                                }
                            }
                            else{
                                for(int i=0; i<6; i++){
                                    Tetris_Array[Array_X, Array_Y-2] = 'a';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X+1, Array_Y+2] = 'a';
                                    Array_Y++;
                                }
                            }
                            GOTOXY(Projection_X-2, Projection_Y);Console.Write("□□□");
                            GOTOXY(Projection_X+2, Projection_Y+1);Console.Write("□");
                            break;
                        case 4:
                            if(Array_Flag){
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X+1, Array_Y-2] = 'J';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X, Array_Y] = 'J';
                                    Tetris_Array[Array_X-1, Array_Y] = 'J';
                                    Array_Y++;
                                }
                            }
                            else{
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X+1, Array_Y-2] = 'a';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X, Array_Y] = 'a';
                                    Tetris_Array[Array_X-1, Array_Y] = 'a';
                                    Array_Y++;
                                }
                            }
                            GOTOXY(Projection_X-2, Projection_Y+1);Console.Write("□□");
                            GOTOXY(Projection_X, Projection_Y);Console.Write("□");
                            GOTOXY(Projection_X, Projection_Y-1);Console.Write("□");
                            break;
                    }
                    break;
                case 'L':
                    if(AS=='+'){
                        Array_X = Projection_Y;
                        Array_Y = Projection_X;
                        Array_Flag = true;
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                    }
                    else if(AS=='-'){
                        Array_X = Projection_Y;
                        Array_Y = Projection_X;
                        Array_Flag = false;
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    switch(N){
                        case 1:
                            if(Array_Flag){
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X-1, Array_Y+2] = 'L';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<6; i++){
                                    Tetris_Array[Array_X, Array_Y-2] = 'L';
                                    Array_Y++;
                                }
                            }
                            else{
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X-1, Array_Y+2] = 'a';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<6; i++){
                                    Tetris_Array[Array_X, Array_Y-2] = 'a';
                                    Array_Y++;
                                }
                            }
                            GOTOXY(Projection_X+2, Projection_Y-1);Console.Write("□");
                            GOTOXY(Projection_X-2, Projection_Y);Console.Write("□□□");
                            break;
                        case 2:
                            if(Array_Flag){
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X+1, Array_Y] = 'L';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X, Array_Y] = 'L';
                                    Tetris_Array[Array_X-1, Array_Y] = 'L';
                                    Array_Y++;
                                }
                            }
                            else{
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X+1, Array_Y] = 'a';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X, Array_Y] = 'a';
                                    Tetris_Array[Array_X-1, Array_Y] = 'a';
                                    Array_Y++;
                                }
                            }
                            GOTOXY(Projection_X, Projection_Y+1);Console.Write("□□");
                            GOTOXY(Projection_X, Projection_Y);Console.Write("□");
                            GOTOXY(Projection_X, Projection_Y-1);Console.Write("□");
                            break;
                        case 3:
                            if(Array_Flag){
                                for(int i=0; i<6; i++){
                                    Tetris_Array[Array_X, Array_Y-2] = 'L';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X+1, Array_Y-2] = 'L';
                                    Array_Y++;
                                }
                            }
                            else{
                                for(int i=0; i<6; i++){
                                    Tetris_Array[Array_X, Array_Y-2] = 'a';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X+1, Array_Y-2] = 'a';
                                    Array_Y++;
                                }
                            }
                            GOTOXY(Projection_X-2, Projection_Y);Console.Write("□□□");
                            GOTOXY(Projection_X-2, Projection_Y+1);Console.Write("□");
                            break;
                        case 4:
                            if(Array_Flag){
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X-1, Array_Y-2] = 'L';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X, Array_Y] = 'L';
                                    Tetris_Array[Array_X+1, Array_Y] = 'L';
                                    Array_Y++;
                                }
                            }
                            else{
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X-1, Array_Y-2] = 'a';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X, Array_Y] = 'a';
                                    Tetris_Array[Array_X+1, Array_Y] = 'a';
                                    Array_Y++;
                                }
                            }
                            GOTOXY(Projection_X-2, Projection_Y-1);Console.Write("□□");
                            GOTOXY(Projection_X, Projection_Y);Console.Write("□");
                            GOTOXY(Projection_X, Projection_Y+1);Console.Write("□");
                            break;
                    }
                    break;
                case 'O':
                    if(AS=='+'){
                        Array_X = Projection_Y;
                        Array_Y = Projection_X;
                        Array_Flag = true;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    else if(AS=='-'){
                        Array_X = Projection_Y;
                        Array_Y = Projection_X;
                        Array_Flag = false;
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    if(Array_Flag){
                        for(int i=0; i<4; i++){
                            Tetris_Array[Array_X-1, Array_Y-2] = 'O';
                            Tetris_Array[Array_X, Array_Y-2] = 'O';
                            Array_Y++;
                        }
                    }
                    else{
                        for(int i=0; i<4; i++){
                            Tetris_Array[Array_X-1, Array_Y-2] = 'a';
                            Tetris_Array[Array_X, Array_Y-2] = 'a';
                            Array_Y++;
                        }
                    }
                    GOTOXY(Projection_X-2, Projection_Y-1);Console.Write("□□");
                    GOTOXY(Projection_X-2, Projection_Y);Console.Write("□□");
                    break;
                case 'S':
                    if(AS=='+'){
                        Array_X = Projection_Y;
                        Array_Y = Projection_X;
                        Array_Flag = true;
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else if(AS=='-'){
                        Array_X = Projection_Y;
                        Array_Y = Projection_X;
                        Array_Flag = false;
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    switch(N){
                        case 1:
                        case 3:
                            if(Array_Flag){
                                for(int i=0; i<4;i ++){
                                    Tetris_Array[Array_X-1, Array_Y] = 'S';
                                    Tetris_Array[Array_X, Array_Y-2] = 'S';
                                    Array_Y++;
                                }
                            }
                            else{
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X-1, Array_Y] = 'a';
                                    Tetris_Array[Array_X, Array_Y-2] = 'a';
                                    Array_Y++;
                                }
                            }
                            GOTOXY(Projection_X, Projection_Y-1);Console.Write("□□");
                            GOTOXY(Projection_X-2, Projection_Y);Console.Write("□□");
                            break;
                        case 2:
                        case 4:
                            if(Array_Flag){
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X, Array_Y] = 'S';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X-1, Array_Y] = 'S';
                                    Tetris_Array[Array_X+1, Array_Y+2] = 'S';
                                    Array_Y++;
                                }
                            }
                            else{
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X, Array_Y] = 'a';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X-1, Array_Y] = 'a';
                                    Tetris_Array[Array_X+1, Array_Y+2] = 'a';
                                    Array_Y++;
                                }
                            }
                            GOTOXY(Projection_X, Projection_Y);Console.Write("□□");
                            GOTOXY(Projection_X, Projection_Y-1);Console.Write("□");
                            GOTOXY(Projection_X+2, Projection_Y+1);Console.Write("□");
                            break;
                    }
                    break;
                case 'T':
                    if(AS=='+'){
                        Array_X = Projection_Y;
                        Array_Y = Projection_X;
                        Array_Flag = true;
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    }
                    else if(AS=='-'){
                        Array_X = Projection_Y;
                        Array_Y = Projection_X;
                        Array_Flag = false;
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    switch(N){
                        case 1:
                            if(Array_Flag){
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X-1, Array_Y] = 'T';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<6; i++){
                                    Tetris_Array[Array_X, Array_Y-2] = 'T';
                                    Array_Y++;
                                }
                            }
                            else{
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X-1, Array_Y] = 'a';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<6; i++){
                                    Tetris_Array[Array_X, Array_Y-2] = 'a';
                                    Array_Y++;
                                }
                            }
                            GOTOXY(Projection_X, Projection_Y-1);Console.Write("□");
                            GOTOXY(Projection_X-2, Projection_Y);Console.Write("□□□");
                            break;
                        case 2:
                            if(Array_Flag){
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X, Array_Y] = 'T';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X-1, Array_Y] = 'T';
                                    Tetris_Array[Array_X+1, Array_Y] = 'T';
                                    Array_Y++;
                                }
                            }
                            else{
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X, Array_Y] = 'a';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X-1, Array_Y] = 'a';
                                    Tetris_Array[Array_X+1, Array_Y] = 'a';
                                    Array_Y++;
                                }
                            }
                            GOTOXY(Projection_X, Projection_Y);Console.Write("□□");
                            GOTOXY(Projection_X, Projection_Y-1);Console.Write("□");
                            GOTOXY(Projection_X, Projection_Y+1);Console.Write("□");
                            break;
                        case 3:
                            if(Array_Flag){
                                for(int i=0; i<6; i++){
                                    Tetris_Array[Array_X, Array_Y-2] = 'T';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X+1, Array_Y] = 'T';
                                    Array_Y++;
                                }
                            }
                            else{
                                for(int i=0; i<6; i++){
                                    Tetris_Array[Array_X, Array_Y-2] = 'a';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X+1, Array_Y] = 'a';
                                    Array_Y++;
                                }
                            }
                            GOTOXY(Projection_X-2, Projection_Y);Console.Write("□□□");
                            GOTOXY(Projection_X, Projection_Y+1);Console.Write("□");
                            break;
                        case 4:
                            if(Array_Flag){
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X, Array_Y-2] = 'T';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X-1, Array_Y] = 'T';
                                    Tetris_Array[Array_X+1, Array_Y] = 'T';
                                    Array_Y++;
                                }
                            }
                            else{
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X, Array_Y-2] = 'a';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X-1, Array_Y] = 'a';
                                    Tetris_Array[Array_X+1, Array_Y] = 'a';
                                    Array_Y++;
                                }
                            }
                            GOTOXY(Projection_X-2, Projection_Y);Console.Write("□□");
                            GOTOXY(Projection_X, Projection_Y-1);Console.Write("□");
                            GOTOXY(Projection_X, Projection_Y+1);Console.Write("□");
                            break;
                    }
                    break;
                case 'Z':
                    if(AS=='+'){
                        Array_X = Projection_Y;
                        Array_Y = Projection_X;
                        Array_Flag = true;
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    else if(AS=='-'){
                        Array_X = Projection_Y;
                        Array_Y = Projection_X;
                        Array_Flag = false;
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    switch(N){
                        case 1:
                        case 3:
                            if(Array_Flag){
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X-1, Array_Y-2] = 'Z';
                                    Tetris_Array[Array_X, Array_Y] = 'Z';
                                    Array_Y++;
                                }
                            }
                            else{
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X-1, Array_Y-2] = 'a';
                                    Tetris_Array[Array_X, Array_Y] = 'a';
                                    Array_Y++;
                                }
                            }
                            GOTOXY(Projection_X-2, Projection_Y-1);Console.Write("□□");
                            GOTOXY(Projection_X, Projection_Y);Console.Write("□□");
                            break;
                        case 2:
                        case 4:
                            if(Array_Flag){
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X, Array_Y-2] = 'Z';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X-1, Array_Y] = 'Z';
                                    Tetris_Array[Array_X+1, Array_Y-2] = 'Z';
                                    Array_Y++;
                                }
                            }
                            else{
                                for(int i=0; i<4; i++){
                                    Tetris_Array[Array_X, Array_Y-2] = 'a';
                                    Array_Y++;
                                }
                                Array_Y = Projection_X;
                                for(int i=0; i<2; i++){
                                    Tetris_Array[Array_X-1, Array_Y] = 'a';
                                    Tetris_Array[Array_X+1, Array_Y-2] = 'a';
                                    Array_Y++;
                                }
                            }
                            GOTOXY(Projection_X-2, Projection_Y);Console.Write("□□");
                            GOTOXY(Projection_X, Projection_Y-1);Console.Write("□");
                            GOTOXY(Projection_X-2, Projection_Y+1);Console.Write("□");
                            break;
                    }
                    break;
                
            }
        }
        // 消除之後重新印方塊的顏色
        static void COLOR(){
            for(int i=1; i<21; i++){
                for(int j=20; j<40; j+=2){
                    switch(Tetris_Array[i,j]){
                        case 'I':
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            GOTOXY(j, i);Console.Write("■");
                            break;
                        case 'J':
                            Console.ForegroundColor = ConsoleColor.DarkBlue;
                            GOTOXY(j, i);Console.Write("■");
                            break;
                        case 'L':
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            GOTOXY(j, i);Console.Write("■");
                            break;
                        case 'O':
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            GOTOXY(j, i);Console.Write("■");
                            break;
                        case 'S':
                            Console.ForegroundColor = ConsoleColor.Green;
                            GOTOXY(j, i);Console.Write("■");
                            break;
                        case 'T':
                            Console.ForegroundColor = ConsoleColor.DarkMagenta;
                            GOTOXY(j, i);Console.Write("■");
                            break;
                        case 'Z':
                            Console.ForegroundColor = ConsoleColor.Red;
                            GOTOXY(j, i);Console.Write("■");
                            break;
                        default:
                            Console.ForegroundColor = ConsoleColor.Black;
                            GOTOXY(j, i);Console.Write("■");
                            break;
                    }
                }
                Console.WriteLine();
            }
        }
        // 判斷是否有消除一直行的方塊
        static void CUBE_DISAPPEAR(){
            int N = 0;
            Score_Add_Time = 0;
            Clean_Flag = false;
            for(int i=1; i<21; i++){
                for(int j=20; j<40; j++){
                    if(Tetris_Array[i,j] != 'a'){
                        N++;
                    }
                    else{
                        N = 0;
                        break;
                    }
                }
                if(N == 20){
                    for(int k=i; k>1; k--){
                        for(int l=20; l<40; l++){
                            Tetris_Array[k,l] = Tetris_Array[k-1,l];
                        }
                    }
                    N = 0;
                    Clean_Flag = true;
                    Score_Add_Time++;
                    Line_Count++;
                }
            }
            if(Clean_Flag){
                Score_Eliminate_Time++;
                Score_Accumulate();
                RANK_SCORE();
                COLOR();
            }
            else{
                Clean_Flag = false;
                Score_Eliminate_Time = -1;
            }
        }
        static void PRINT_ARRAY(){   
            GOTOXY(61,0);
            for(int i=1; i<21; i++){
                for(int j=20; j<40; j++){
                    Console.Write(Tetris_Array[i,j]);
                }
                GOTOXY(61,i);Console.WriteLine();
            }
        }
        static void Score_Accumulate(){
            switch(Score_Add_Time){
                case 1:
                    if(Score_Eliminate_Time == -1 || Score_Eliminate_Time == 0){
                        Score = Score+10;
                    }
                    else{
                        Score = Score+10+(10*Score_Eliminate_Time);
                    }
                    break;
                case 2:
                    if(Score_Eliminate_Time == -1 || Score_Eliminate_Time == 0){
                        Score = Score+15;
                    }
                    else{
                        Score = Score+15+(10*Score_Eliminate_Time);
                    }
                    break;
                case 3:
                    if(Score_Eliminate_Time == -1 || Score_Eliminate_Time == 0){
                        Score = Score+18;
                    }
                    else{
                        Score = Score+18+(10*Score_Eliminate_Time);
                    }
                    break;
                case 4:
                    if(Score_Eliminate_Time == -1 || Score_Eliminate_Time == 0){
                        Score = Score+20;
                    }
                    else{
                        Score = Score+20+(10*Score_Eliminate_Time);
                    }
                    break;        
            }
            if(Score>100 && Score<=300 && Judge_Flag==true){
                Score = Score+100;
                UPLEVEL();
                Judge_Flag = false;
            }
            else if(Score>300 && Score<=600 && Judge_Flag==false){
                Score = Score+200;
                UPLEVEL();
                Judge_Flag = true;
            }
            else if(Score>600 && Score<=1100 && Judge_Flag==true){
                Score = Score+300;
                UPLEVEL();
                Judge_Flag = false;
            }
            else if(Score>1100 && Score<=1700 && Judge_Flag==false){
                Score = Score+400;
                UPLEVEL();
                Judge_Flag = true;
            }
            else if(Score>1700 && Score<=2400 && Judge_Flag==true){
                Score = Score+500;
                UPLEVEL();
                Judge_Flag = false;
            }
            else if(Score>2400 && Score<=3200 && Judge_Flag==false){
                Score = Score+600;
                UPLEVEL();
                Judge_Flag = true;
            }
            else if(Score>3200 && Score<=4100 && Judge_Flag==true){
                Score = Score+700;
                UPLEVEL();
                Judge_Flag = false;
            }
            else if(Score>4100 && Score<=5100 && Judge_Flag==false){
                Score = Score+800;
                UPLEVEL();
                Judge_Flag = true;
            }
            else if(Score>5100 && Judge_Flag==true){
                Score = Score+900;
                UPLEVEL();
                Judge_Flag = false;
            }
        }
        // 判斷Level並減少秒數
        static void UPLEVEL(){
            if(Level!=10){
                Level++;
                Sec = Sec-100;
            }
            else{
                Sec = 100;
            }
        }
        // 選擇鍵
        static void SELECT_PLAY(string End_Or_Level){
            int X = 20, Y = 9; 
            bool Play = false;
            WORD_SELECT('+', X, Y);
            while( !Play ){    
                ConsoleKeyInfo k = Console.ReadKey();          
                switch(k.Key.ToString()){
                    case "UpArrow":
                        WORD_SELECT('-', X, Y);
                        Y = 9;
                        WORD_SELECT('+', X, Y);
                        GOTOXY(0, 0);
                        break;
                    case "DownArrow":
                        WORD_SELECT('-', X, Y);
                        Y = 15;
                        WORD_SELECT('+', X, Y);
                        GOTOXY(0, 0);
                        break;
                    case "Spacebar":
                        if( Y==9 ){
                            Play = true;
                            End = false;
                            return;
                        }
                        else if( Y==15 ){
                            switch(End_Or_Level){
                                case "LEVEL":
                                    End = false;
                                    if( ++Level==11 ){
                                        Level = 1;
                                        Sec = 1000;
                                    }
                                    else{
                                        Sec = Sec-100;
                                    }
                                    NUMBER('-', 0, 33, 15);
                                    NUMBER('+', Level/10, 33, 15);
                                    NUMBER('-', 0, 37, 15);
                                    NUMBER('+', Level%10, 37, 15);
                                    Console.BackgroundColor = ConsoleColor.Black;
                                    Console.ForegroundColor = ConsoleColor.White;
                                    GOTOXY(2, 16);Console.Write("            ");
                                    GOTOXY(7, 16);Console.Write(Level);
                                    break;
                                case "END":
                                    End = true;
                                    Play = true;
                                    break;
                                default:
                                    break;
                            }
                        }
                        GOTOXY(0, 0);
                        break;
                    default:
                        break;
                }
            }
        }
        // 連接資料庫
        static void DATABASE_HIGHSCORE(){
            String connetStr = "server="+IP+";port=3306;user=root;password=Evannight055660; database=tetris;";//user=hi;password=1qaz@WSX;
            MySqlConnection conn = new MySqlConnection(connetStr);
            MySqlCommand command = conn.CreateCommand();
            string sql = "select * from userdata;";
            MySqlCommand cmd = new MySqlCommand(sql,conn);
            MySqlDataReader data;
            Count = 0;
            for(int i=0; i<20; i++){
                HighScore[i] = -1;
                HighScoreName[i] = " ";
            }
            try{    
                conn.Open();//開啟通道，建立連線，可能出現異常,使用try catch語句
                data = cmd.ExecuteReader();
                while(data.Read()){
                    GET_HIGHSCORE(data);
                }
                conn.Close();
            }
            catch (MySqlException ex){
                Console.WriteLine(ex.Message);
            }
            finally{
                conn.Close();
            }
            Array.Sort(HighScoreName, HighScore);
            Array.Sort(HighScore, HighScoreName);
            Array.Reverse(HighScore);
            Array.Reverse(HighScoreName);
        }
        // 連結資料庫
        static void DATABASE_SIGN_ACCOUNT(){
            String connetStr = "server="+IP+";port=3306;user="+User+";password="+Pwd+"; database=tetris;";
            MySqlConnection conn = new MySqlConnection(connetStr);
            MySqlCommand command = conn.CreateCommand();
            GOTOXY(8,7);Console.WriteLine("歡迎來到Tetris註冊系統，請依照下面步驟註冊。");
            bool account_flag = false;
            while(account_flag == false){
                account_flag = true;
                GOTOXY(8,9);Console.Write("請輸入帳號（8個英文字母或數字的組合）：");
                GOTOXY(45,9);User_Account = Console.ReadLine();
                GOTOXY(8,10);Console.WriteLine("                                 ");
                if(User_Account.Length < 8 ){
                    Thread.Sleep(10);
                    GOTOXY(8,10);Console.WriteLine("帳號名稱過短！請重新輸入。");GOTOXY(45,9);Console.Write("        ");//34
                    account_flag = false;
                    continue;
                }
                else{
                    conn.Open();
                    string sql = "select account from userdata;";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    MySqlDataReader data = cmd.ExecuteReader();
                    while(data.Read()){
                        if((string)data["account"] == User_Account){
                            Thread.Sleep(10);
                            GOTOXY(8,10);Console.WriteLine("帳號名稱已有人使用！請重新輸入。");GOTOXY(45,9);Console.Write("        ");//40
                            account_flag = false;
                            break;
                        }
                    }
                    conn.Close();
                    continue;
                }
                conn.Close();
            }
        }
        // 連接資料庫註冊密碼
        static void DATABASE_SIGN_PASSWORD(){
            bool password_flag = false;
            while(password_flag == false){
                password_flag = true;
                User_Password = null;
                GOTOXY(8,11);Console.WriteLine("請輸入密碼（8個英文字母或數字的組合）：");//34
                GOTOXY(45,11);User_Password = INPUT_PASSWORD(User_Password);
                GOTOXY(8,12);Console.Write("                             ");
                if(User_Password.Length < 8 ){
                    Thread.Sleep(10);
                    GOTOXY(8,12);Console.WriteLine("密碼過短！請重新輸入。");GOTOXY(45,11);Console.Write("        ");//30
                    password_flag = false;
                    continue;
                }
            }
        }
        // 連接資料庫註冊暱稱
        static void DATABASE_SIGN_NICKNAME(){
            String connetStr = "server="+IP+";port=3306;user="+User+";password="+Pwd+"; database=tetris;";
            MySqlConnection conn = new MySqlConnection(connetStr);
            MySqlCommand command = conn.CreateCommand();
            bool nickname_flag = false;
            while(nickname_flag == false){
                nickname_flag = true;
                GOTOXY(8,13);Console.WriteLine("請輸入暱稱（至多3字元）：");
                GOTOXY(31,13);User_Nickname = Console.ReadLine();
                GOTOXY(8,14);Console.Write("                                   ");
                if(User_Nickname.Length > 3 ){
                    Thread.Sleep(10);
                    GOTOXY(8,14);Console.WriteLine("暱稱過長！請重新輸入。");GOTOXY(31,13);Console.Write("      ");
                    nickname_flag = false;
                    continue;
                }
                else{
                    conn.Open();
                    string sql = "select nickname from userdata;";
                    MySqlCommand cmd = new MySqlCommand(sql,conn);
                    MySqlDataReader data = cmd.ExecuteReader();
                    while(data.Read()){
                        if((string)data["nickname"] == User_Nickname){
                            Thread.Sleep(10);
                            GOTOXY(8,14);Console.WriteLine("暱稱已有人使用！請重新輸入。");GOTOXY(31,13);Console.Write("      ");//36
                            nickname_flag = false;
                            break;
                        }
                    }
                    conn.Close();
                    continue;
                }
                conn.Close();
            }
        }
        // 連接資料庫註冊
        static void DATABASE_SIGN(){
            String connetStr = "server="+IP+";port=3306;user="+User+";password="+Pwd+"; database=tetris;";
            MySqlConnection conn = new MySqlConnection(connetStr);
            MySqlCommand command = conn.CreateCommand();
            conn.Open();
            string sql = string.Empty;
            sql = "insert into userdata(account,password,highscore,nickname) values('"+User_Account+"','"+User_Password+"','0','"+User_Nickname+"');";
            MySqlCommand cmd = new MySqlCommand(sql,conn);
            cmd.ExecuteNonQuery();
            conn.Close();
            GOTOXY(8,16);Console.WriteLine("註冊成功！請重新登入！");
            Console.ReadKey();
            Console.Clear();
        }
        static void DATABASE_LOGIN(){
            String connetStr = "server="+IP+";port=3306;user="+User+";password="+Pwd+"; database=tetris;";
            MySqlConnection conn = new MySqlConnection(connetStr);
            MySqlCommand command = conn.CreateCommand();
            GOTOXY(8,7);Console.WriteLine("歡迎來到Tetris登入系統，請輸入Tetris帳號密碼。");
            while(log_flag == true){
                conn.Open();
                string sql = "select * from userdata;";
                MySqlCommand cmd = new MySqlCommand(sql,conn);
                MySqlDataReader data = cmd.ExecuteReader();
                User_Password = null;
                GOTOXY(16,9);Console.Write("您的帳號：");
                GOTOXY(26,9);User_Account = Console.ReadLine();
                GOTOXY(16,11);Console.Write("您的密碼：");
                GOTOXY(26,11);User_Password = INPUT_PASSWORD(User_Password);
                GOTOXY(16,13);Console.Write("                                     ");
                while(data.Read()){
                    if((string)data["account"]==User_Account){
                        if((string)data["password"]==User_Password){
                            User_Nickname = (string)data["nickname"];
                            User_Highscore = (int)data["highscore"];
                            GOTOXY(16,14);Console.Write("登入成功！歡迎您，{0}",User_Nickname);
                            GOTOXY(16,15);Console.Write("您的最高分為 {0}",User_Highscore);
                            log_flag=false;
                            break;
                        }
                    }
                }
                if(log_flag==true){
                    Thread.Sleep(10);
                    GOTOXY(16,13);Console.WriteLine("登入失敗！帳號或密碼錯誤，請重新輸入。");GOTOXY(26,9);Console.Write("        ");GOTOXY(26,11);Console.Write("        ");
                    conn.Close();
                }
            }
            Console.ReadKey();
            Console.Clear();
            conn.Close();
        }
        // 從資料庫的data中取得暱稱與分數的資料
        static void GET_HIGHSCORE(MySqlDataReader data){
            HighScore[Count] = (int)data["highscore"];
            HighScoreName[Count++] = (string)data["nickname"];
        }
    }     
}