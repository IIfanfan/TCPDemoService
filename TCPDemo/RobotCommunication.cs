using RP.ScoutRobot.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace RP.ScoutRobot.Communication
{
    public class RobotCommunication : IRobotCommunication
    {
        private const int DefaultTimeout = 1000;//默认超时时间
        private TcpSocketClient socket;//socket通信客户端

        public RobotCommunication(string ipAddr,int port,int timeout=-1)
        {
            socket = new TcpSocketClient(ipAddr, port, timeout == -1 ? DefaultTimeout : timeout);
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="common">命令码</param>
        /// <returns></returns>
        public ComResult<RT> Send<RT>(int common) where RT : struct
        {
            ComResult<RT> result = new ComResult<RT>();
            try
            {
                //发送命令码
                byte[] data = BitConverter.GetBytes(common);

                //发送数据
                byte[] sendData = new byte[11];
                sendData[0] = 0xff;
                sendData[1] = 0x01;
                sendData[2] = 0x00;
                Array.Copy(data, 0, sendData, 3, 4);
                sendData[7] = 0x01;
                Array.Copy(BitConverter.GetBytes(0), 0, sendData, 8, 2);
                sendData[10] = DataCheck(sendData);

                //准备接收缓冲区
                int receiveDataSize = Marshal.SizeOf(typeof(RT));

                //发送数据
                var receiveDate = SendData(sendData, receiveDataSize);

                //if (receiveDataSize==233)
                //{
                //    result.Result = ComResultEnum.Success;
                //}

                if (receiveDate[0] == 0xff&& receiveDate[2]==0x01)
                {
                    //校验数据
                    if (receiveDate[receiveDate.Length - 1] != DataCheck(receiveDate))
                    {
                        result.Result = ComResultEnum.VerificationError;
                    }

                    //解析数据
                    int dataLength = BitConverter.ToInt16(receiveDate, 8); //获取数据长度
                    if(dataLength== receiveDataSize)
                    {
                        byte[] dataBytes = new byte[dataLength];
                        Array.Copy(receiveDate, 10, dataBytes, 0, dataLength);
                        result.Data = MarshalHelper.BytesToStruct<RT>(dataBytes);
                    }
                    else
                    {
                        result.Result = ComResultEnum.VerificationError;
                    }
                }
                else
                {
                    result.Result = ComResultEnum.VerificationError;
                }
            }
            catch(Exception ex)
            {
                result.Result = ComResultEnum.TimeOut;
            }            

            return result;
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="common">发送数据</param>
        /// <param name="data">命令码</param>
        /// <returns></returns>
        public ComResult<RT> Send<T, RT>(int common, T data)
            where T : struct
            where RT : struct
        {
            ComResult<RT> result = new ComResult<RT>();
            try
            {
                //发送命令码
                byte[] commonData = BitConverter.GetBytes(common);
                byte[] sendStruct = MarshalHelper.StructToBytes(data);

                //发送数据
                byte[] sendData = new byte[sendStruct.Length+11];
                sendData[0] = 0xff;
                sendData[1] = 0x01;
                sendData[2] = 0x00;
                Array.Copy(commonData, 0, sendData, 3, 4);
                sendData[7] = 0x01;
                Array.Copy(BitConverter.GetBytes(sendStruct.Length), 0, sendData, 8, 2);
                Array.Copy(sendStruct, 0, sendData, 10, sendStruct.Length);
                sendData[sendData.Length-1] = DataCheck(sendData);

                //准备接收缓冲区
                int receiveDataSize = Marshal.SizeOf(typeof(RT));

                //发送数据
                var receiveDate=SendData(sendData, receiveDataSize);

                if (receiveDate[0] == 0xff && receiveDate[2] == 0x01)
                {
                    //校验数据
                    if (receiveDate[receiveDate.Length - 1] != DataCheck(receiveDate))
                    {
                        result.Result = ComResultEnum.VerificationError;
                    }

                    //解析数据
                    int dataLength = BitConverter.ToInt16(receiveDate, 8); //获取数据长度
                    if (dataLength == receiveDataSize)
                    {
                        byte[] dataBytes = new byte[dataLength];
                        Array.Copy(receiveDate, 10, dataBytes, 0, dataLength);
                        result.Data = MarshalHelper.BytesToStruct<RT>(dataBytes);
                    }
                    else
                    {
                        result.Result = ComResultEnum.VerificationError;
                    }
                }
                else
                {
                    result.Result = ComResultEnum.VerificationError;
                }
            }
            catch (Exception ex)
            {
                result.Result = ComResultEnum.TimeOut;
            }

            return result;
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="sendData"></param>
        /// <param name="receiveDataSize"></param>
        /// <returns></returns>
        private byte[] SendData(byte[] sendData,int receiveDataSize) 
        {
            //准备接收缓冲区
            byte[] receiveDate = new byte[receiveDataSize + 11];

            if (!socket.IsOpen)
            {
                socket.Open();

                SocketInit();//发送命令，表明身份
                Thread.Sleep(100);
            }

            //发送并接收数据
            socket.SendData(sendData, ref receiveDate);

            return receiveDate;
        }

        /// <summary>
        /// 通信协议初始化
        /// </summary>
        private void SocketInit()
        {
            //发送命令码
            byte[] data = BitConverter.GetBytes(0);

            //发送数据
            byte[] sendData = new byte[11];
            sendData[0] = 0xff;
            sendData[1] = 0x01;
            sendData[2] = 0x00;
            Array.Copy(data, 0, sendData, 3, 4);
            sendData[7] = 0x01;
            Array.Copy(BitConverter.GetBytes(0), 0, sendData, 8, 2);
            sendData[10] = DataCheck(sendData);

            socket.SendData(sendData);
        }

        /// <summary>
        /// 计算校验和
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private byte DataCheck(byte[] data)
        {
            int crc = 0;
            for(int i = 0; i < data.Length - 1; i++)
            {
                crc += data[i];
            }

            return (byte)(crc%256);
        }
    }
}
