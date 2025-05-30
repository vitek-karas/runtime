// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/**************************************************************************/
/* a binary string (blob) class */

#ifndef BINSTR_H
#define BINSTR_H

#include <string.h>         // for memmove, memcpy ...

class BinStr {
public:
    BinStr()  { len = 0L; max = 8L; ptr_ = buff; }
    BinStr(BYTE* pb, DWORD cb) { len = cb; max = cb+8; ptr_ = pb; }
    ~BinStr() { if (ptr_ != buff) delete [] ptr_;   }

    void insertInt8(int val) { if (len >= max) Realloc(); memmove(ptr_+1, ptr_, len); *ptr_ = (uint8_t)val; len++; }
    void insertInt32(int val) { if (len + 4 > max) Realloc(); memmove(ptr_+4, ptr_, len); SET_UNALIGNED_32(&ptr_[0], val); len+=4; }
    void appendInt8(int val) { if (len >= max) Realloc(); ptr_[len++] = (uint8_t)val; }
    void appendInt16(int val) { if (len + 2 > max) Realloc(); SET_UNALIGNED_16(&ptr_[len], val); len += 2; }
    void appendInt32(int val) { if (len + 4 > max) Realloc(); SET_UNALIGNED_32(&ptr_[len], val); len += 4; }
    void appendInt64(int64_t *pval) { if (len + 8 > max) Realloc(8); SET_UNALIGNED_64(&ptr_[len],(*pval)); len += 8; }
    uint8_t* getBuff(unsigned size) {
        if (len + size > max) Realloc(size);
        _ASSERTE(len + size <= max);
        uint8_t* ret = &ptr_[len];
        len += size;
        return(ret);
        }
    void append(BinStr* str) {
       memcpy(getBuff(str->length()), str->ptr(), str->length());
       }

    void appendFrom(BinStr* str, unsigned ix) {
        _ASSERTE(str->length() >= ix);
        if (str->length() >= ix)
        {
            memcpy(getBuff(str->length()-ix), str->ptr()+ix, str->length()-ix);
        }
       }

    void remove(unsigned size) { _ASSERTE(len >= size); len -= size; }

    uint8_t* ptr()      { return(ptr_); }
    unsigned length()   { return(len); }

private:
    void Realloc(unsigned atLeast = 4) {
        max = max * 2;
        if (max < atLeast + len)
            max = atLeast + len;
        _ASSERTE(max >= len + atLeast);
        uint8_t* newPtr = new uint8_t[max];
        memcpy(newPtr, ptr_, len);
        if (ptr_ != buff) delete [] ptr_;
        ptr_ = newPtr;
        }

private:
    unsigned  len;
    unsigned  max;
    uint8_t *ptr_;
    uint8_t buff[8];
};
BinStr* BinStrToUnicode(BinStr* pSource, bool Swap = false);

#endif

