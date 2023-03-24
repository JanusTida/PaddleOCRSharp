// Copyright (c) 2021 raoyutian Authors. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System;
using System.Linq;
using System.Drawing.Imaging;
using System.IO;
 

namespace PaddleOCRSharp
{
    /// <summary>
    /// PaddleOCR识别引擎对象
    /// </summary>
    public class PaddleOCREngine : IDisposable
    {
        #region PaddleOCR API
        [DllImport("PaddleOCR.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        internal static extern IntPtr Initialize(string det_infer, string cls_infer, string rec_infer, string keys, OCRParameter parameter);

        [DllImport("PaddleOCR.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        internal static extern int Detect(IntPtr engine, string imagefile, out IntPtr result);

        [DllImport("PaddleOCR.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        internal static extern int DetectByte(IntPtr engine, byte[] imagebytedata,long size, out IntPtr result);

        [DllImport("PaddleOCR.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        internal static extern int DetectBase64(IntPtr engine, string imagebase64, out IntPtr result);

        [DllImport("PaddleOCR.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        internal static extern int FreeEngine(IntPtr enginePtr);
        [DllImport("PaddleOCR.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        internal static extern int FreeDetectResult(IntPtr intPtr);


        [DllImport("PaddleOCR.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        internal static extern void DetectImage(string det_infer, string imagefile, OCRParameter parameter);

        /// <summary>
        /// 检测当前CPU硬件是否支持OCR
        /// </summary>
        /// <returns> 0：不支持，1：AVX，2：AVX2</returns>
        [DllImport("PaddleOCR.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        internal static extern int IsCPUSupport();
        #endregion

        #region 属性
        /// <summary>
        /// OCR识别引擎指针
        /// </summary>
        private IntPtr Engine;
        
        #endregion

        #region 文本识别
        /// <summary>
        /// PaddleOCR识别引擎对象初始化
        /// </summary>
        /// <param name="config">模型配置对象，如果为空则按默认值</param>
        /// <param name="parameter">识别参数，为空均按缺省值</param>
        public PaddleOCREngine(OCRModelConfig config, OCRParameter parameter = null)
        {   
            //0：不支持，1：AVX，2：AVX2
            if (IsCPUSupport() <= 0) throw new NotSupportedException($"The CPU instruction set is not surpport PaddleOCR");
            
         
            CheckLibFiles();

            if (parameter == null) parameter = new OCRParameter();
            if (config == null)
            {
                string root = GetRootDirectory();
                config = new OCRModelConfig();
                string modelPathroot = root + @"\inference";
                config.det_infer = modelPathroot + @"\ch_PP-OCRv3_det_infer";
                config.cls_infer = modelPathroot + @"\ch_ppocr_mobile_v2.0_cls_infer";
                config.rec_infer = modelPathroot + @"\ch_PP-OCRv3_rec_infer";
                config.keys = modelPathroot + @"\ppocr_keys.txt";
            }
            if (!Directory.Exists(config.det_infer)) throw new DirectoryNotFoundException(config.det_infer);
            if (!Directory.Exists(config.cls_infer)) throw new DirectoryNotFoundException(config.cls_infer);
            if (!Directory.Exists(config.rec_infer)) throw new DirectoryNotFoundException(config.rec_infer);
            if (!File.Exists(config.keys)) throw new FileNotFoundException(config.keys);
            Engine = Initialize(config.det_infer, config.cls_infer, config.rec_infer, config.keys, parameter);

        }
       
        /// <summary>
        /// 对图像文件进行文本识别
        /// </summary>
        /// <param name="imagefile">图像文件</param>
        /// <returns>OCR识别结果</returns>
        public OCRResult DetectText(string imagefile)
        {
            if (!System.IO.File.Exists(imagefile)) throw new Exception($"文件{imagefile}不存在");
            var imagebyte= File.ReadAllBytes(imagefile);
            return DetectText(imagebyte);
        }

        /// <summary>
        ///对图像对象进行文本识别
        /// </summary>
        /// <param name="image">图像</param>
        /// <returns>OCR识别结果</returns>
        [Obsolete("下一版将移除该方法,并计划放在windows扩展程序集中", false)]
        public OCRResult DetectText(Image image)
        {
            if (image == null) throw new ArgumentNullException("image");
            var imagebyte = ImageToBytes(image);
            return DetectText(imagebyte);
        }
      
        /// <summary>
        ///文本识别
        /// </summary>
        /// <param name="imagebyte">图像内存流</param>
        /// <returns>OCR识别结果</returns>
        public OCRResult DetectText(byte[] imagebyte)
        {
             
            if (imagebyte == null) throw new ArgumentNullException("imagebyte");

            IntPtr ptrResult;
            int count  = DetectByte(Engine, imagebyte, imagebyte.LongLength,out ptrResult);
            if (count == 0) return new OCRResult();
            return ConvertResult(ptrResult);
        }

        /// <summary>
        ///文本识别
        /// </summary>
        /// <param name="imagebase64">图像base64</param>
        /// <returns>OCR识别结果</returns>
        public OCRResult DetectTextBase64( string imagebase64)
        {
            
            if (imagebase64==null || imagebase64=="") throw new ArgumentNullException("imagebase64");
            IntPtr ptrResult;
            int count = DetectBase64(Engine, imagebase64, out ptrResult);
            if (count == 0) return new OCRResult();
            return ConvertResult(ptrResult);
        }
      
        /// <summary>
        /// 结果解析
        /// </summary>
        /// <param name="ptrResult"></param>
        /// <returns></returns>
        private OCRResult ConvertResult(IntPtr ptrResult)
        {
            if (ptrResult == IntPtr.Zero) return new OCRResult();
            OCRResult oCRResult = new OCRResult();
            IntPtr ptrFree = ptrResult;
            try
            {
                int textBlockAmount = (int)Marshal.PtrToStructure(ptrResult, typeof(int));
                //总textBlock个数

#if NET35
                ptrResult = (IntPtr)(ptrResult.ToInt64() + 4);
#else
                ptrResult = ptrResult + 4;
#endif

                IntPtr ptrtextBlock = (IntPtr)Marshal.PtrToStructure(ptrResult, typeof(IntPtr));
                ptrResult = ptrtextBlock;
                for (int i = 0; i < textBlockAmount; i++)
                {
                    //文本长度
                    int textBlockLen = (int)Marshal.PtrToStructure(ptrResult, typeof(int));
#if NET35
                    ptrResult = (IntPtr)(ptrResult.ToInt64() + 4);
#else
                    ptrResult = ptrResult + 4;
#endif

                    //文本指针
                    IntPtr textPointValue = (IntPtr)Marshal.PtrToStructure(ptrResult, typeof(IntPtr));

                    //文本
                    TextBlock textBlock = new TextBlock();
                    textBlock.Text = Marshal.PtrToStringUni(textPointValue);

                    //文本四个点
#if NET35
                    ptrResult = (IntPtr)(ptrResult.ToInt64() + 8);
#else
                    ptrResult = ptrResult +8;
#endif
                    for (int p = 0; p < 4; p++)
                    {
                        OCRPoint oCRPoint = (OCRPoint)Marshal.PtrToStructure(ptrResult, typeof(OCRPoint));
                        textBlock.BoxPoints.Add(new OCRPoint(oCRPoint.X, oCRPoint.Y));

#if NET35
                        ptrResult = (IntPtr)(ptrResult.ToInt64() + Marshal.SizeOf(typeof(OCRPoint)));
#else
                        ptrResult = ptrResult + Marshal.SizeOf(typeof(OCRPoint));
#endif
                    }

                    //得分
                    float score = (float)Marshal.PtrToStructure(ptrResult, typeof(float));
                    textBlock.Score = score;
#if NET35
                    ptrResult = (IntPtr)(ptrResult.ToInt64() + 4);
#else
                    ptrResult = ptrResult + 4;
#endif

                    oCRResult.TextBlocks.Add(textBlock);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                FreeDetectResult(ptrFree);
            }
            oCRResult.TextBlocks.Reverse();
            return oCRResult;
        }

        #endregion

        #region 表格识别

        /// <summary>
        ///结构化文本识别
        /// </summary>
        /// <param name="image">图像</param>
        /// <param name="parameter">参数</param>
        /// <returns>表格识别结果</returns>
        [Obsolete("下一版将移除该方法,并计划放在windows扩展程序集中", false)]
        public OCRStructureResult DetectStructure(Image image)
        {
            
            if (image == null) throw new ArgumentNullException("image");
            var imagebyte = ImageToBytes(image);
            OCRResult result= DetectText(imagebyte);
            List<TextBlock> blocks = result.TextBlocks;
            if (blocks == null || blocks.Count == 0) return new OCRStructureResult();
            var listys = getzeroindexs(blocks.OrderBy(x => x.BoxPoints[0].Y).Select(x => x.BoxPoints[0].Y).ToArray(), 10);
            var listxs = getzeroindexs(blocks.OrderBy(x => x.BoxPoints[0].X).Select(x => x.BoxPoints[0].X).ToArray(), 10);

            int rowcount = listys.Count;
            int colcount = listxs.Count;
            OCRStructureResult structureResult = new OCRStructureResult();
            structureResult.TextBlocks = blocks;
            structureResult.RowCount = rowcount;
            structureResult.ColCount = colcount;
            structureResult.Cells = new List<StructureCells>();
            for (int i = 0; i < rowcount; i++)
            {
                int y_min = blocks.OrderBy(x => x.BoxPoints[0].Y).OrderBy(x => x.BoxPoints[0].Y).ToList()[listys[i]].BoxPoints[0].Y;
                int y_max = 99999;
                if (i < rowcount - 1)
                {
                    y_max = blocks.OrderBy(x => x.BoxPoints[0].Y).ToList()[listys[i + 1]].BoxPoints[0].Y;
                }

                for (int j = 0; j < colcount; j++)
                {
                    int x_min = blocks.OrderBy(x => x.BoxPoints[0].X).ToList()[listxs[j]].BoxPoints[0].X;
                    int x_max = 99999;

                    if (j < colcount - 1)
                    {
                        x_max = blocks.OrderBy(x => x.BoxPoints[0].X).ToList()[listxs[j + 1]].BoxPoints[0].X;
                    }

                    var textBlocks = blocks.Where(x => x.BoxPoints[0].X < x_max && x.BoxPoints[0].X >= x_min && x.BoxPoints[0].Y < y_max && x.BoxPoints[0].Y >= y_min).OrderBy(u => u.BoxPoints[0].X);
                    var texts = textBlocks.Select(x => x.Text).ToArray();

                    StructureCells cell = new StructureCells();
                    cell.Row = i;
                    cell.Col = j;

#if NET35
                    cell.Text = string.Join("", texts);
#else
                    cell.Text = string.Join<string>("", texts);
#endif


                    cell.TextBlocks = textBlocks.ToList();
                    structureResult.Cells.Add(cell);
                }
            }
            return structureResult;
        }
       
        /// <summary>
        /// 计算表格分割
        /// </summary>
        /// <param name="pixellist"></param>
        /// <param name="thresholdtozero"></param>
        /// <returns></returns>
        private List<int> getzeroindexs(int[] pixellist, int thresholdtozero = 20)
        {
            List<int> zerolist = new List<int>();
            zerolist.Add(0);
            for (int i = 0; i < pixellist.Length; i++)
            {
                if ((i < pixellist.Length - 1)
                    && (Math.Abs(pixellist[i + 1] - pixellist[i])) > thresholdtozero)
                {
                    //突增点
                    zerolist.Add(i + 1);
                }
            }
            return zerolist;
        }

        #endregion

        #region 预测

        /// <summary>
        ///仅文本预测，在当前文件夹下保存文件名为ocr_vis.png的预测结果
        /// </summary>
        /// <param name="config">模型配置对象，如果为空则按默认值</param>
        /// <param name="imagefile">检测图像全路径</param>
        /// <param name="parameter">参数</param>
        [Obsolete("下一版将移除该方法",true)]
        public static void Detect(OCRModelConfig config, string imagefile, OCRParameter parameter=null)
        {
            if (parameter == null) parameter = new OCRParameter();
            if (config == null)
            {
                string root = GetRootDirectory();
                config = new OCRModelConfig();
                string modelPathroot = root + @"\inference";
                config.det_infer = modelPathroot + @"\ch_PP-OCRv3_det_infer";
                config.cls_infer = modelPathroot + @"\ch_ppocr_mobile_v2.0_cls_infer";
                config.rec_infer = modelPathroot + @"\ch_PP-OCRv3_rec_infer";
                config.keys = modelPathroot + @"\ppocr_keys.txt";
            }
            if (!Directory.Exists(config.det_infer)) throw new DirectoryNotFoundException(config.det_infer);
            if (!File.Exists(imagefile)) throw new FileNotFoundException(imagefile);
            DetectImage(config.det_infer, imagefile, parameter);
        }

        #endregion

        #region private

        /// <summary>
        /// 获取程序的当前路径;
        /// </summary>
        /// <returns></returns>
        private static string GetRootDirectory()
        {
#if NET46_OR_GREATER || NETCOREAPP
            return AppContext.BaseDirectory;
#else
            return Path.GetDirectoryName(typeof(PaddleOCREngine).Assembly.Location);
#endif
        }
        /// <summary>
        /// 依赖文件检查
        /// </summary>
        private void CheckLibFiles()
        {
            string rootpath = GetRootDirectory();
            rootpath= rootpath.TrimEnd('\\')+'\\';
            string[] checkfiles = new string[] { "libiomp5md.dll", "mkldnn.dll", "mklml.dll", "paddle_inference.dll", "PaddleOCR.dll" };
            foreach (var file in checkfiles)
            {
                if (!File.Exists(rootpath+file)) throw new FileNotFoundException(file);
            }
        }

        /// <summary>
        /// Convert Image to Byte[]
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        [Obsolete("下一版将移除该方法,并计划放在windows扩展程序集中", false)]
        private   byte[] ImageToBytes(Image image)
        {
            ImageFormat format = image.RawFormat;
            using (MemoryStream ms = new MemoryStream())
            {
                if (format.Guid == ImageFormat.Jpeg.Guid)
                {
                    image.Save(ms, ImageFormat.Jpeg);
                }
                else if (format.Guid == ImageFormat.Png.Guid)
                {
                    image.Save(ms, ImageFormat.Png);
                }
                else if (format.Guid == ImageFormat.Bmp.Guid)
                {
                    image.Save(ms, ImageFormat.Bmp);
                }
                else if (format.Guid == ImageFormat.Gif.Guid)
                {
                    image.Save(ms, ImageFormat.Gif);
                }
                else if (format.Guid == ImageFormat.Icon.Guid)
                {
                    image.Save(ms, ImageFormat.Icon);
                }
                else
                {
                    image.Save(ms, ImageFormat.Png);
                }
                byte[] buffer = new byte[ms.Length];
                ms.Seek(0, SeekOrigin.Begin);
                ms.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }
#endregion

#region Dispose

        public void Dispose()
        {
            FreeEngine(Engine);
        }
#endregion
    }
}