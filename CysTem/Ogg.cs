using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace CysTem
{
    internal class Ogg
    {
        byte[] OriginalData;
        byte[] Key;
        int ExtraOffsetDisplacement;

        public Ogg(string OriginalFile, string Key, int ExtraOffsetDisplacement)
        {
            OriginalData = File.ReadAllBytes(OriginalFile);
            this.Key = Encoding.ASCII.GetBytes(Key);
            this.ExtraOffsetDisplacement = ExtraOffsetDisplacement;
        }

        public byte[] Decompile()
        {
            byte[] Header = new byte[Math.Min(0xE1F, OriginalData.Length)];
            int CurrentOffset = 0;
            if (Header.Length < 16)
            {
                throw new Exception("File not decompiled correctly");
            }
            Buffer.BlockCopy(OriginalData, CurrentOffset, Header, 0, 16);
            CurrentOffset += 16;

            //Now we proceed to obtain the key for decrypting the OGG file,
            //which will vary depending on its signature
            string Signature = Encoding.ASCII.GetString(Header, 0, 4);
            ExtraOffsetDisplacement = 0;
            if (Signature == "Tink")
            {
                Key = Encoding.ASCII.GetBytes("DBB3206F-F171-4885-A131-EC7FBA6FF491 Copyright 2004 Cyberworks " +
                    "\"TinkerBell\"., all rights reserved.\x00");
            }
            else if (Signature == "Song")
            {
                Key = Encoding.ASCII.GetBytes("49233ED4911E48c68EBF1DDACE3A7752A8B52D3D13C34e509FBE-E3EFDE3F2D61");
            }
            else
            {
                Signature = Encoding.ASCII.GetString(Header, 12, 4);
                Buffer.BlockCopy(OriginalData, CurrentOffset, Header, 4, 12);
                CurrentOffset += 12;
                ExtraOffsetDisplacement = 12;
                if (Signature == "Tink")
                {
                    Key = Encoding.ASCII.GetBytes("DBB3206F-F171-4885-A131-EC7FBA6FF491 Copyright 2004 Cyberworks " +
                        "\"TinkerBell\"., all rights reserved.\x00");
                }
                else if (Signature == "Song")
                {
                    Key = Encoding.ASCII.GetBytes("49233ED4911E48c68EBF1DDACE3A7752A8B52D3D13C34e509FBE-E3EFDE3F2D61");
                }
                else
                {
                    throw new Exception("File not decompiled correctly");
                }
            }

            //The first four bytes follow the OGG Vorbis structure
            Header[0] = (byte)'O';
            Header[1] = (byte)'g';
            Header[2] = (byte)'g';
            Header[3] = (byte)'S';

            //Now we copy the remaining bytes from the original audio onto the header
            Buffer.BlockCopy(OriginalData, CurrentOffset, Header, 16, Header.Length - 16);

            //Now we XOR each of the bytes (except the first four bytes that we have set up before)
            //with the key obtained before
            int DecryptionKeyIndex = 0;
            for (int CurrentByte = 4; CurrentByte < Header.Length; CurrentByte++)
            {
                Header[CurrentByte] ^= Key[DecryptionKeyIndex++];
                if (DecryptionKeyIndex >= Key.Length)
                    DecryptionKeyIndex = 1;
            }

            //After decrypting the header, now we need to create the complete file with the remaining bytes
            if (Header.Length >= OriginalData.Length)
            {
                return Header;
            }
            else
            {
                byte[] UncompressedFile = new byte[OriginalData.Length - ExtraOffsetDisplacement];
                Buffer.BlockCopy(Header, 0, UncompressedFile, 0, Header.Length);
                Buffer.BlockCopy(OriginalData, Header.Length + ExtraOffsetDisplacement, UncompressedFile, Header.Length,
                    OriginalData.Length - Header.Length - ExtraOffsetDisplacement);
                return UncompressedFile;
            }
        }

        public byte[] Compile()
        {
            byte[] Header = new byte[Math.Min(0xE1F, OriginalData.Length)];
            Buffer.BlockCopy(OriginalData, 0, Header, 0, Header.Length);

            if (OriginalData.Length <= 4 && Encoding.ASCII.GetString(OriginalData, 0, 4) != "OggS")
            {
                throw new Exception("Invalid OGG file");
            }

            if (Key.SequenceEqual(Encoding.ASCII.GetBytes("DBB3206F-F171-4885-A131-EC7FBA6FF491 Copyright 2004 Cyberworks " +
                    "\"TinkerBell\"., all rights reserved.\x00")))
            {
                Header[0] = (byte)'T';
                Header[1] = (byte)'i';
                Header[2] = (byte)'n';
                Header[3] = (byte)'k';
            }

            else if (Key.SequenceEqual(Encoding.ASCII.GetBytes("49233ED4911E48c68EBF1DDACE3A7752A8B52D3D13C34e509FBE-E3EFDE3F2D61")))
            {
                Header[0] = (byte)'S';
                Header[1] = (byte)'o';
                Header[2] = (byte)'n';
                Header[3] = (byte)'g';
            }

            int EncryptionKeyIndex = 0;
            for (int CurrentByte = 4; CurrentByte < Header.Length; CurrentByte++)
            {
                Header[CurrentByte] ^= Key[EncryptionKeyIndex++];
                if (EncryptionKeyIndex >= Key.Length)
                    EncryptionKeyIndex = 1;
            }

            //After decrypting the header, now we need to create the complete file with the remaining bytes
            if (Header.Length >= OriginalData.Length)
            {
                return Header;
            }
            else
            {
                byte[] UncompressedFile = new byte[OriginalData.Length + ExtraOffsetDisplacement];
                Buffer.BlockCopy(Header, 0, UncompressedFile, ExtraOffsetDisplacement, Header.Length);
                Buffer.BlockCopy(OriginalData, Header.Length, UncompressedFile, Header.Length + ExtraOffsetDisplacement,
                    OriginalData.Length - Header.Length - ExtraOffsetDisplacement);
                return UncompressedFile;
            }
        }

        //It returns a string array of the values of the file that we had initially, only needed when
        //reconstructing an uncompressed file
        public string[] GetMetadata()
        {
            string[] Metadata = new string[2];
            Metadata[0] = Encoding.ASCII.GetString(Key);
            Metadata[1] = Convert.ToString(ExtraOffsetDisplacement);
            return Metadata;
        }
    }
}
