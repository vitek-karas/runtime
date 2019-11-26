// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// Utils.cpp
//


//
// Implements a bunch of binder auxilary functions
//
// ============================================================

#include "utils.hpp"

#include "strongname.h"
#include "corpriv.h"

namespace BINDER_SPACE
{
    namespace
    {
        inline const WCHAR *GetPlatformPathSeparator()
        {
#ifdef PLATFORM_UNIX
            return W("/");
#else
            return W("\\");
#endif // PLATFORM_UNIX
        }
    }

    void CombinePath(SString &pathA,
                     SString &pathB,
                     SString &combinedPath)
    {
        SString platformPathSeparator(SString::Literal, GetPlatformPathSeparator());
        combinedPath.Set(pathA);

        if (!combinedPath.EndsWith(platformPathSeparator))
        {
            combinedPath.Append(platformPathSeparator);
        }

        combinedPath.Append(pathB);
    }

    HRESULT GetTokenFromPublicKey(SBuffer &publicKeyBLOB,
                                  SBuffer &publicKeyTokenBLOB)
    {
        HRESULT hr = S_OK;

        const BYTE *pByteKey = publicKeyBLOB;
        DWORD dwKeyLen = publicKeyBLOB.GetSize();
        BYTE *pByteToken = NULL;
        DWORD dwTokenLen = 0;

        if (!StrongNameTokenFromPublicKey(const_cast<BYTE *>(pByteKey),
                                          dwKeyLen,
                                          &pByteToken,
                                          &dwTokenLen))
        {
            IF_FAIL_GO(StrongNameErrorInfo());
        }
        else
        {
            _ASSERTE(pByteToken != NULL);
            publicKeyTokenBLOB.Set(pByteToken, dwTokenLen);
            StrongNameFreeBuffer(pByteToken);
        }

    Exit:
        return hr;
    }

    BOOL IsFileNotFound(HRESULT hr)
    {
        return RuntimeFileNotFound(hr);
    }
};
