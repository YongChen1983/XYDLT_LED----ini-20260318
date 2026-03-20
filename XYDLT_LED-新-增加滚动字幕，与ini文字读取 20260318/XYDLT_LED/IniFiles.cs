using System;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;


    public   class  IniFiles 
    {
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        /// &lt;summary&gt;
        /// 获取某个指定节点(Section)中所有KEY和Value
        /// &lt;/summary&gt;
        /// &lt;param name&#61;&#34;lpAppName&#34;&gt;节点名称&lt;/param&gt;
        /// &lt;param name&#61;&#34;lpReturnedString&#34;&gt;返回值的内存地址,每个之间用\0分隔&lt;/param&gt;
        /// &lt;param name&#61;&#34;nSize&#34;&gt;内存大小(characters)&lt;/param&gt;
        /// &lt;param name&#61;&#34;lpFileName&#34;&gt;Ini文件&lt;/param&gt;
        /// &lt;returns&gt;内容的实际长度,为0表示没有内容,为nSize-2表示内存大小不够&lt;/returns&gt;
        [DllImport("kernel32.dll", CharSet=CharSet.Auto)]



    private static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, [In, Out] char[] lpReturnedString, uint nSize, string lpFileName);

    //另一种声明方式,使用 StringBuilder 作为缓冲区类型的缺点是不能接受\0字符，会将\0及其后的字符截断,
    //所以对于lpAppName或lpKeyName为null的情况就不适用
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);

    //再一种声明，使用string作为缓冲区的类型同char[]
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, string lpReturnedString, uint nSize, string lpFileName);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern uint GetPrivateProfileSection(string lpAppName, IntPtr lpReturnedString, uint nSize, string lpFileName);
        public IniFiles(string inipath)
        {
            this.inipath = inipath;
        }

        /// &lt;summary&gt;
        /// 获取INI文件中指定节点(Section)中的所有条目(key&#61;value形式)
        /// &lt;/summary&gt;
        /// &lt;param name&#61;&#34;iniFile&#34;&gt;Ini文件&lt;/param&gt;
        /// &lt;param name&#61;&#34;section&#34;&gt;节点名称&lt;/param&gt;
        /// &lt;returns&gt;指定节点中的所有项目,没有内容返回string[0]&lt;/returns&gt;
        public static string[] INIGetAllItems(string iniFile, string section)
        {
            //返回值形式为 key&#61;value,例如 Color&#61;Red
            uint MAX_BUFFER = 32767;    //默认为32767

            string[] items = new string[0];      //返回值

            //分配内存
            IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)MAX_BUFFER * sizeof(char));

            uint bytesReturned = IniFiles.GetPrivateProfileSection(section, pReturnedString, MAX_BUFFER, iniFile);

            if (!(bytesReturned == MAX_BUFFER - 2) || (bytesReturned==0))
            {

                string returnedString=Marshal.PtrToStringAuto(pReturnedString, (int)bytesReturned);
                items = returnedString.Split(new char[] {'\0' }, StringSplitOptions.RemoveEmptyEntries);
            }

            Marshal.FreeCoTaskMem(pReturnedString);     //释放内存

            return items;
        }




        public string inipath ;
        /// <summary> 
        /// 写入INI文件 
        /// </summary> 
        /// <param name="Section">项目名称(如 [TypeName] )</param> 
        /// <param name="Key">键</param> 
        /// <param name="Value">值</param> 
        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.inipath);
        }

    public static string INIGetStringValue(string iniFile, string section, string key, string defaultValue)
    {
        string value = defaultValue;
        const int SIZE = 1024 * 10;

        if (string.IsNullOrEmpty(section))
        {
            throw new ArgumentException("必须指定节点名称", "section");
        }

        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("必须指定键名称(key)", "key");
        }

        StringBuilder sb = new StringBuilder(SIZE);
        int bytesReturned = IniFiles.GetPrivateProfileString(section, key, defaultValue, sb, SIZE, iniFile);

        if (bytesReturned != 0)
        {
            value = sb.ToString();
        }
        sb = null;

        return value;
    }





        /// <summary> 
        /// 读出INI文件 
        /// </summary> 
        /// <param name="Section">项目名称(如 [TypeName] )</param> 
        /// <param name="Key">键</param> 
        public string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(500);
            int i = GetPrivateProfileString(Section, Key, "", temp, 500, this.inipath);
            return temp.ToString();

        }
        /// <summary> 
        /// 验证文件是否存在 
        /// </summary> 
        /// <returns>布尔值</returns> 
        public bool ExistINIFile()
        {
            return File.Exists(inipath);
        }
    }

