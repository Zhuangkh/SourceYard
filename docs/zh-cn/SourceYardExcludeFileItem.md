# SourceYardExcludeFileItem

��Ҫ�� SourceYard ����������ļ����Щ�ļ���ڴ��ʱ���ų�

������һЩ�ĵ���д�뵽��Ŀ���棬���������ʱ�򣬲�Ҫ����Щ�ĵ������ӵ�Դ������������棬���ⱻ���

��������һЩ����ʹ�õ��ļ������� foo.coin �ļ�����������Ϊ��Դ��������Ŀ����

����������Դ��������õ��ļ�����뵽 SourceYardExcludeFileItem �б����ڴ����ʱ�򱻺������ã����ļ���Ȼ�ᱻ��ӵ�Դ�������棬ֻ�����ö���

���뷽������

```xml
  <ItemGroup>
    <SourceYardExcludeFileItem Include="foo.coin" />
    <SourceYardExcludeFileItem Include="Resource\F1.md" />
  </ItemGroup>
```