Name: kensaku
Version: <VERSION>
Release: 1%{?dist}
Summary: Quick and easy search for Japanese kanji, radicals, and words
License: MIT
URL: https://github.com/LiteracyFanatic/kensaku
Requires: dotnet-runtime-9.0

%global __strip /bin/true
%define debug_package %{nil}

%description
Quick and easy search for Japanese kanji, radicals, and words

%prep
mkdir -p %{_builddir}/kensaku
cd %{_builddir}/kensaku
wget --quiet -O kensaku "https://github.com/LiteracyFanatic/kensaku/releases/download/v%{version}/kensaku-linux-x64"
wget --quiet -O kensaku.db "https://github.com/LiteracyFanatic/kensaku/releases/download/v%{version}/kensaku.db"
chmod +x kensaku

%build

%install
mkdir -p %{buildroot}/usr/bin
install -m 0755 %{_builddir}/kensaku/kensaku %{buildroot}/usr/bin/kensaku
mkdir -p %{buildroot}/usr/share/kensaku
install -m 0644 %{_builddir}/kensaku/kensaku.db %{buildroot}/usr/share/kensaku/kensaku.db

%files
/usr/bin/kensaku
/usr/share/kensaku/kensaku.db
