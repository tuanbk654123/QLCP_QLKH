import React, { useState, useCallback, useEffect } from 'react';
import {
  Card,
  Upload,
  Form,
  Input,
  Button,
  Row,
  Col,
  Typography,
  message,
  Space,
  Tabs
} from 'antd';
import {
  UploadOutlined,
  DownloadOutlined,
  PlusOutlined,
  MinusCircleOutlined,
  FileWordOutlined
} from '@ant-design/icons';
import axios from 'axios';
import { useAuth } from '../../context/AuthContext';

const { Title } = Typography;

const ExportWord = () => {
  const [form] = Form.useForm();
  const [exportLoading, setExportLoading] = useState(false);
  const [fieldPermissions, setFieldPermissions] = useState({});
  const { isAdmin } = useAuth();

  const fetchPermissions = useCallback(async () => {
    try {
      const response = await axios.get('/api/permissions/current', {
        params: { module: 'qlkh' },
      });
      setFieldPermissions(response.data.permissions || {});
    } catch (error) {
      setFieldPermissions({});
    }
  }, []);

  useEffect(() => {
    fetchPermissions();
  }, [fetchPermissions]);

  const canReadField = (field) => {
    if (isAdmin && isAdmin()) return true;
    const level = fieldPermissions[field];
    return level && level !== 'N';
  };

  const canEditField = (field) => {
    if (isAdmin && isAdmin()) return true;
    const level = fieldPermissions[field];
    return level === 'W' || level === 'A';
  };

  const handleExportWord = async (index) => {
    const values = form.getFieldsValue();
    const template = values.templates?.[index];

    if (!template) return;

    // Handle File List from Form
    const fileList = template.file;
    const file = fileList && fileList.length > 0 ? fileList[0].originFileObj : null;

    if (!file) {
      message.error(`Template ${index + 1}: Vui lòng chọn file template`);
      return;
    }

    const formData = new FormData();
    formData.append('file', file);

    const payload = (template.dataSets || []).map(ds => {
      const dict = {};
      if (ds.name) {
        dict['_FileName'] = ds.name;
      }
      (template.fields || []).forEach((field, fIndex) => {
        const key = `text_${fIndex + 1}`;
        const val = ds.values?.[fIndex] || '';
        dict[key] = val;
      });
      return dict;
    });

    if (payload.length === 0) {
        message.error(`Template ${index + 1}: Vui lòng thêm ít nhất một bộ dữ liệu`);
        return;
    }

    formData.append('data', JSON.stringify(payload));

    try {
      setExportLoading(true);
      const token = localStorage.getItem('authToken');
      const response = await axios.post('/api/export/word', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
          'Authorization': `Bearer ${token}`
        },
        responseType: 'blob',
      });

      const contentType = response.headers['content-type'];
      const extension = contentType.includes('zip') ? 'zip' : 'docx';
      const defaultName = extension === 'zip' ? `exported_documents_${index + 1}.zip` : `exported_document_${index + 1}.docx`;

      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', defaultName);
      document.body.appendChild(link);
      link.click();
      link.remove();
      message.success(`Template ${index + 1}: Xuất file thành công`);
    } catch (error) {
      console.error('Export error:', error);
      message.error(`Template ${index + 1}: Lỗi khi xuất file`);
    } finally {
      setExportLoading(false);
    }
  };

  const normFile = (e) => {
    if (Array.isArray(e)) {
      return e;
    }
    return e?.fileList;
  };

  if (!canReadField('export_doc')) {
    return (
        <Card>
            <div style={{ color: 'red', textAlign: 'center', padding: '20px' }}>
                Bạn không có quyền truy cập chức năng này.
            </div>
        </Card>
    );
  }

  return (
    <Card 
        title={<Space><FileWordOutlined /><Title level={4} style={{ marginBottom: 0 }}>Xuất văn bản</Title></Space>}
        style={{ margin: '24px' }}
    >
      <div style={{ overflowX: 'auto' }}>
          <Form 
            layout="vertical" 
            form={form} 
            initialValues={{ 
                templates: [{
                    fields: [{ key: 'text_1' }], 
                    dataSets: [{ name: 'File_1', values: [] }] 
                }]
            }}
          >
            <Form.List name="templates">
              {(fields, { add, remove }) => (
                <Tabs
                  type="editable-card"
                  onEdit={(targetKey, action) => {
                    if (action === 'add') {
                      add({ 
                        fields: [{ key: 'text_1' }], 
                        dataSets: [{ name: 'File_1', values: [] }] 
                      });
                    } else {
                      const field = fields.find(f => f.key.toString() === targetKey);
                      if (field) remove(field.name);
                    }
                  }}
                  items={fields.map((field, index) => ({
                    label: `Template ${index + 1}`,
                    key: field.key.toString(),
                    closable: fields.length > 1,
                    children: (
                      <div style={{ padding: '16px 0' }}>
                        <Form.Item 
                          label="File Template Word" 
                          required
                          name={[field.name, 'file']}
                          valuePropName="fileList"
                          getValueFromEvent={normFile}
                        >
                          <Upload
                            beforeUpload={() => false}
                            maxCount={1}
                            accept=".docx"
                          >
                            <Button icon={<UploadOutlined />}>Chọn file template</Button>
                          </Upload>
                        </Form.Item>

                        <div style={{ display: 'flex', gap: '16px', paddingBottom: '16px' }}>
                            {/* Column 1: Field Definitions */}
                            <div style={{ minWidth: '200px', flexShrink: 0, borderRight: '1px solid #f0f0f0', paddingRight: '16px' }}>
                                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px', alignItems: 'center' }}>
                                    <span style={{ fontWeight: 'bold', fontSize: '16px' }}>Trường dữ liệu</span>
                                </div>
                                
                                <div style={{ 
                                    marginBottom: '16px', 
                                    padding: '4px', 
                                    background: '#e6f7ff', 
                                    borderRadius: '4px',
                                    border: '1px dashed #1890ff',
                                    height: '56px', 
                                    display: 'flex',
                                    alignItems: 'center',
                                    justifyContent: 'center'
                                }}>
                                    <span style={{ fontWeight: 'bold', color: '#1890ff' }}>Tên file xuất ra</span>
                                </div>

                                <Form.List name={[field.name, 'fields']}>
                                    {(subFields, { add: addField, remove: removeField }) => (
                                        <>
                                            {subFields.map(({ key, name: subName, ...restSubField }, subIndex) => (
                                                <div key={key} style={{ height: '56px', display: 'flex', alignItems: 'center', marginBottom: '8px' }}>
                                                    <Space>
                                                        <span style={{ fontWeight: 500 }}>text_{subIndex + 1}</span>
                                                        {subFields.length > 1 && (
                                                            <MinusCircleOutlined onClick={() => removeField(subName)} style={{ color: 'red' }} />
                                                        )}
                                                    </Space>
                                                </div>
                                            ))}
                                            <Button type="dashed" onClick={() => addField({})} block icon={<PlusOutlined />}>
                                                Thêm trường
                                            </Button>
                                        </>
                                    )}
                                </Form.List>
                            </div>

                            {/* Columns N: Data Sets */}
                            <Form.List name={[field.name, 'dataSets']}>
                                {(dataSets, { add: addDataSet, remove: removeDataSet }) => (
                                    <>
                                        {dataSets.map(({ key, name: dsName, ...restDsField }, colIndex) => (
                                            <div key={key} style={{ minWidth: '250px', flexShrink: 0, background: '#fafafa', padding: '12px', borderRadius: '8px' }}>
                                                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px', alignItems: 'center' }}>
                                                    <span style={{ fontWeight: 'bold' }}>Bộ dữ liệu {colIndex + 1}</span>
                                                    {dataSets.length > 1 && (
                                                        <MinusCircleOutlined onClick={() => removeDataSet(dsName)} style={{ color: 'red', cursor: 'pointer' }} />
                                                    )}
                                                </div>
                                                <Form.Item
                                                    {...restDsField}
                                                    name={[dsName, 'name']}
                                                    initialValue={`File_${colIndex + 1}`}
                                                    rules={[{ required: true, message: 'Nhập tên file' }]}
                                                    style={{ 
                                                        marginBottom: '16px', 
                                                        padding: '4px', 
                                                        background: '#e6f7ff', 
                                                        borderRadius: '4px',
                                                        border: '1px dashed #1890ff',
                                                        height: '56px',
                                                        display: 'flex',
                                                        flexDirection: 'column',
                                                        justifyContent: 'center'
                                                    }}
                                                >
                                                    <Input 
                                                        prefix={<FileWordOutlined style={{ color: '#1890ff' }} />}
                                                        placeholder="Nhập tên file" 
                                                        style={{ fontWeight: '500' }}
                                                    />
                                                </Form.Item>
                                                
                                                <Form.Item
                                                    noStyle
                                                    shouldUpdate={(prev, curr) => {
                                                        // Deep compare or check specific path changes
                                                        // Since we are inside a map, accessing templates[index].fields is tricky in shouldUpdate
                                                        // But simply returning true or checking form reference is safer for now.
                                                        // We need to re-render when the *current template's fields* change.
                                                        return true; 
                                                    }}
                                                >
                                                    {({ getFieldValue }) => {
                                                        const currentTemplate = getFieldValue(['templates', field.name]) || {};
                                                        const currentFields = currentTemplate.fields || [];
                                                        
                                                        return currentFields.map((f, rowIndex) => (
                                                            <Form.Item
                                                                key={f?.key || rowIndex}
                                                                name={[dsName, 'values', rowIndex]}
                                                                style={{ marginBottom: '8px', height: '56px' }}
                                                            >
                                                                <Input placeholder={`Nhập giá trị...`} />
                                                            </Form.Item>
                                                        ));
                                                    }}
                                                </Form.Item>
                                            </div>
                                        ))}
                                        <div style={{ minWidth: '100px', display: 'flex', alignItems: 'flex-start' }}>
                                            <Button type="dashed" onClick={() => addDataSet({ values: [] })} icon={<PlusOutlined />} style={{ height: '100%' }}>
                                                Thêm bộ dữ liệu
                                            </Button>
                                        </div>
                                    </>
                                )}
                            </Form.List>
                        </div>

                        <Form.Item style={{ marginTop: '24px' }}>
                            <Button 
                                type="primary" 
                                icon={<DownloadOutlined />} 
                                onClick={() => handleExportWord(field.name)} 
                                loading={exportLoading}
                                disabled={!canEditField('export_doc')}
                                block
                                size="large"
                            >
                                Xuất dữ liệu (Template {index + 1})
                            </Button>
                        </Form.Item>
                      </div>
                    )
                  }))}
                />
              )}
            </Form.List>
          </Form>
      </div>
    </Card>
  );
};

export default ExportWord;
